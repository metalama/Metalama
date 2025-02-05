// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.UserCode;
using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Attribute = System.Attribute;
using TypedConstant = Microsoft.CodeAnalysis.TypedConstant;
using TypeKind = Microsoft.CodeAnalysis.TypeKind;

namespace Metalama.Framework.Engine.CompileTime;

internal abstract class AttributeDeserializer : IAttributeDeserializer
{
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly CompileTimeTypeResolver _compileTimeTypeResolver;
    private readonly CompilationContext _compilationContext;
    private readonly UserCodeInvoker _userCodeInvoker;

    protected AttributeDeserializer(
        in ProjectServiceProvider serviceProvider,
        CompileTimeTypeResolver compileTimeTypeResolver,
        CompilationContext compilationContext )
    {
        this._serviceProvider = serviceProvider;
        this._compileTimeTypeResolver = compileTimeTypeResolver;
        this._compilationContext = compilationContext;
        this._userCodeInvoker = serviceProvider.GetRequiredService<UserCodeInvoker>();
    }

    // Coverage: ignore
    public bool TryCreateAttribute<T>( AttributeData attribute, IDiagnosticAdder diagnosticAdder, [NotNullWhen( true )] out T? attributeInstance )
        where T : Attribute
    {
        if ( this.TryCreateAttribute( attribute, diagnosticAdder, out var untypedAttribute ) )
        {
            attributeInstance = (T) untypedAttribute;

            return true;
        }
        else
        {
            attributeInstance = null;

            return false;
        }
    }

    public bool TryCreateAttribute( IAttribute attribute, IDiagnosticAdder diagnosticAdder, [NotNullWhen( true )] out Attribute? attributeInstance )
        => this.TryCreateAttribute( attribute.GetAttributeData(), diagnosticAdder, out attributeInstance );

    public bool TryCreateAttribute( AttributeData attribute, IDiagnosticAdder diagnosticAdder, [NotNullWhen( true )] out Attribute? attributeInstance )
    {
        var constructorSymbol = attribute.AttributeConstructor;

        if ( constructorSymbol == null )
        {
            // This may happen at design time or with an invalid file. In this case, no error message is necessary.

            attributeInstance = null;

            return false;
        }

        var type = this._compileTimeTypeResolver.GetCompileTimeType( constructorSymbol.ContainingType, false );

        if ( type == null )
        {
            diagnosticAdder.Report(
                AttributeDeserializerDiagnostics.CannotFindAttributeType.CreateRoslynDiagnostic(
                    attribute.GetDiagnosticLocation(),
                    constructorSymbol.ContainingType ) );

            attributeInstance = null;

            return false;
        }

        return this.TryCreateAttribute( attribute, type, diagnosticAdder, out attributeInstance! );
    }

    private bool TryCreateAttribute(
        AttributeData attribute,
        Type type,
        IDiagnosticAdder diagnosticAdder,
        [NotNullWhen( true )] out Attribute? attributeInstance )
    {
        var constructorSymbol = attribute.AttributeConstructor!;

        var constructors = type.GetConstructors().Where( c => this.ParametersMatch( c.GetParameters(), constructorSymbol.Parameters ) ).ToReadOnlyList();

        if ( constructors.Count == 0 )
        {
            throw new AssertionFailedException( $"Cannot map find the ConstructorInfo for '{constructorSymbol}'." );
        }
        else if ( constructors.Count > 1 )
        {
            throw new AssertionFailedException( $"Found more than one ConstructorInfo for '{constructorSymbol}'." );
        }

        var constructor = constructors[0];

        var constructorParameters = constructor.GetParameters();
        var arguments = new object?[constructorParameters.Length];
        ParameterInfo? paramsParameter = null;
        Array? paramsArgument = null;
        var paramsIndex = 0;

        if ( constructorParameters.Length > 0 )
        {
            // This code seems to execute only at design time.

            var lastParameter = constructorSymbol.Parameters[constructorParameters.Length - 1];

            if ( lastParameter.IsParams && constructorParameters.Length != attribute.ConstructorArguments.Length )
            {
                paramsParameter = constructorParameters[constructorParameters.Length - 1];

                paramsArgument = Array.CreateInstance(
                    paramsParameter.ParameterType.GetElementType()!,
                    attribute.ConstructorArguments.Length - constructorParameters.Length + 1 );
            }
        }

        for ( var i = 0; i < attribute.ConstructorArguments.Length; i++ )
        {
            var constructorArgument = attribute.ConstructorArguments[i];
            var isInParams = paramsParameter != null && i >= paramsParameter.Position;

            var parameterInfo = isInParams ? paramsParameter! : constructorParameters[i];

            if ( !this.TryTranslateAttributeArgument(
                    attribute,
                    constructorArgument,
                    parameterInfo.ParameterType,
                    out var translatedArgument ) )
            {
                attributeInstance = null;

                return false;
            }

            if ( isInParams )
            {
                if ( translatedArgument == null ||
                     paramsArgument!.GetType().GetElementType()!.IsInstanceOfType( translatedArgument ) )
                {
                    paramsArgument!.SetValue( translatedArgument, paramsIndex );
                    paramsIndex++;
                }
            }
            else
            {
                arguments[i] = translatedArgument;
            }
        }

        if ( paramsArgument != null )
        {
            arguments[paramsParameter!.Position] = paramsArgument;
        }

        var executionContext = UserCodeExecutionContext.CreateInstance(
            this._serviceProvider,
            UserCodeDescription.Create( "calling the {0} constructor while instantiating a custom attribute", constructor ),
            this._compilationContext,
            diagnostics: diagnosticAdder );

        if ( !this._userCodeInvoker.TryInvoke( () => (Attribute) constructor.Invoke( arguments ), executionContext, out var localAttributeInstance ) )
        {
            attributeInstance = null;

            return false;
        }

        foreach ( var arg in attribute.NamedArguments )
        {
            if ( arg.Value.Kind == TypedConstantKind.Error )
            {
                // Ignore a field or property assigned to a value that could not be parsed to the correct type by Roslyn.
                continue;
            }

            if ( type.GetProperty( arg.Key ) is { } property )
            {
                if ( !this.TryTranslateAttributeArgument( attribute, arg.Value, property.PropertyType, out var translatedValue ) )
                {
                    attributeInstance = null;

                    return false;
                }

                var setter = property.SetMethod ?? property.GetSetMethod( true );

                if ( setter == null )
                {
                    diagnosticAdder.Report(
                        AttributeDeserializerDiagnostics.PropertyHasNoSetter.CreateRoslynDiagnostic(
                            attribute.GetDiagnosticLocation(),
                            arg.Key ) );

                    attributeInstance = null;

                    return false;
                }

                if ( !this._userCodeInvoker.TryInvoke(
                        () => setter.Invoke( localAttributeInstance, [translatedValue] ),
                        executionContext.WithDescription(
                            UserCodeDescription.Create( "setting the {0} property while instantiating a custom attribute", property ) ) ) )
                {
                    attributeInstance = null;

                    return false;
                }
            }
            else if ( type.GetField( arg.Key ) is { } field )
            {
                if ( !this.TryTranslateAttributeArgument( attribute, arg.Value, field.FieldType, out var translatedValue ) )
                {
                    attributeInstance = null;

                    return false;
                }

                if ( !this._userCodeInvoker.TryInvoke(
                        () => field.SetValue( localAttributeInstance, translatedValue ),
                        executionContext.WithDescription(
                            UserCodeDescription.Create( "setting the {0} field while instantiating a custom attribute", field ) ) ) )
                {
                    attributeInstance = null;

                    return false;
                }
            }
            else
            {
                // Template properties (incl. introduced ones) may be removed from compile-time compilation,
                // so they would not be found above, but are already reported as LAMA0257.
                // We should not get an invalid member otherwise.
                throw new AssertionFailedException( $"Cannot find a FieldInfo or PropertyInfo named '{arg.Key}'." );
            }
        }

        attributeInstance = localAttributeInstance;

        return true;
    }

    private bool TryTranslateAttributeArgument(
        AttributeData attribute,
        TypedConstant typedConstant,
        Type targetType,
        out object? translatedValue )
    {
        object? value;

        switch ( typedConstant.Kind )
        {
            case TypedConstantKind.Error:
                // This expresses when invalid expression is used in an attribute, e.g.: int.Parse("42").
                translatedValue = null;

                return false;

            case TypedConstantKind.Array when typedConstant.Values.IsDefault:
                translatedValue = null;

                return true;

            case TypedConstantKind.Array:
                value = typedConstant.Values;

                break;

            default:
                value = typedConstant.Value;

                break;
        }

        return this.TryTranslateAttributeArgument(
            attribute,
            value,
            typedConstant.Type,
            targetType,
            out translatedValue );
    }

    private bool TryTranslateAttributeArgument(
        AttributeData attribute,
        object? value,
        ITypeSymbol? sourceType,
        Type targetType,
        out object? translatedValue )
    {
        if ( value == null )
        {
            translatedValue = null;

            return true;
        }

        switch ( value )
        {
            case TypedConstant typedConstant:
                return this.TryTranslateAttributeArgument( attribute, typedConstant, targetType, out translatedValue );

            case IErrorTypeSymbol:
                translatedValue = null;

                return false;

            case ITypeSymbol type:

                var compileTimeTime = this._compileTimeTypeResolver.GetCompileTimeType( type, true );

                if ( targetType.IsAssignableFrom( typeof(Type) ) || targetType.IsAssignableFrom( typeof(Type[]) ) )
                {
                    translatedValue = compileTimeTime;

                    return true;
                }
                else
                {
                    translatedValue = null;

                    return false;
                }

            case string str:
                // Make sure we don't fall under the IEnumerable case.
                if ( !targetType.IsAssignableFrom( typeof(string) ) )
                {
                    // This should not happen because we don't process invalid values.
                    throw new AssertionFailedException( $"Cannot convert '{value.GetType().Name}' to '{targetType.Name}'." );
                }

                translatedValue = str;

                return true;

            case IEnumerable enumerable:
                // We cannot use generic collections here because array of value types are not convertible to arrays of objects.

                var list = enumerable.ToReadOnlyList();
                var elementType = targetType.GetElementType() ?? typeof(object);

                var count = 0;

                foreach ( var unused in list )
                {
                    count++;
                }

                var array = Array.CreateInstance( elementType, count );

                var index = 0;

                foreach ( var item in list )
                {
                    if ( !this.TryTranslateAttributeArgument( attribute, item, sourceType, elementType, out var translatedItem ) )
                    {
                        translatedValue = null;

                        return false;
                    }

                    array.SetValue( translatedItem, index );
                    index++;
                }

                translatedValue = array;

                return true;

            default:
                if ( sourceType is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType )
                {
                    // Convert the underlying value of an enum to a strongly typed enum when we can.
                    var enumReflectionType = this._compileTimeTypeResolver.GetCompileTimeType( enumType, false );

                    if ( enumReflectionType != null )
                    {
                        value = Enum.ToObject( enumReflectionType, value );
                    }
                }

                if ( !targetType.IsInstanceOfType( value ) )
                {
                    translatedValue = value;

                    return false;
                }

                translatedValue = value;

                return true;
        }
    }

    private bool ParametersMatch( ParameterInfo[] reflectionParameters, ImmutableArray<IParameterSymbol> roslynParameters )
    {
        if ( reflectionParameters.Length != roslynParameters.Length )
        {
            return false;
        }

        for ( var i = 0; i < reflectionParameters.Length; i++ )
        {
            var compileTimeType = this._compileTimeTypeResolver.GetCompileTimeType( roslynParameters[i].Type, true );

            if ( reflectionParameters[i].ParameterType != compileTimeType )
            {
                return false;
            }
        }

        return true;
    }
}