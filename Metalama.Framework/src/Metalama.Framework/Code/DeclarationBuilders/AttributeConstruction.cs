// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Code.DeclarationBuilders
{
    /// <summary>
    /// Encapsulates the information necessary to create a custom attribute programmatically.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this class to create custom attributes when introducing them to declarations via <see cref="AdviserExtensions.IntroduceAttribute"/>
    /// or when adding them to introduced declarations via <see cref="IDeclarationBuilder.AddAttribute"/>.
    /// </para>
    /// <para>
    /// The <see cref="Create(IConstructor, IReadOnlyList{TypedConstant}?, IReadOnlyList{KeyValuePair{string, TypedConstant}}?)"/> method
    /// creates an attribute by specifying an explicit constructor and strongly-typed <see cref="TypedConstant"/> arguments.
    /// The <see cref="Create(Type, IReadOnlyList{object?}?, IReadOnlyList{KeyValuePair{string, object?}}?)"/> and
    /// <see cref="Create(INamedType, IReadOnlyList{object?}?, IReadOnlyList{KeyValuePair{string, object?}}?)"/> overloads
    /// automatically find a suitable constructor based on the provided argument types.
    /// </para>
    /// <para>
    /// To copy an existing attribute from the code model, use the <see cref="IAttribute"/> directly as it implements <see cref="IAttributeData"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="IAttribute"/>
    /// <seealso cref="IAttributeData"/>
    /// <seealso cref="IDeclarationBuilder.AddAttribute"/>
    /// <seealso cref="AdviserExtensions.IntroduceAttribute"/>
    /// <seealso cref="AttributeExtensions"/>
    /// <seealso href="@adding-attributes"/>
    public sealed class AttributeConstruction : IAttributeData
    {
        /// <summary>
        /// Gets the attribute constructor.
        /// </summary>
        public IConstructor Constructor { get; }

        /// <summary>
        /// Gets the attribute type.
        /// </summary>
        public INamedType Type => this.Constructor.DeclaringType;

        /// <summary>
        /// Gets the constructor arguments.
        /// </summary>
        public ImmutableArray<TypedConstant> ConstructorArguments { get; }

        /// <summary>
        /// Gets the named arguments, i.e. the assigned fields and properties.
        /// Note that the order may be important in case of non-trivial property setters.
        /// </summary>
        public INamedArgumentList NamedArguments { get; }

        private AttributeConstruction(
            IConstructor constructor,
            IReadOnlyList<TypedConstant>? constructorArguments,
            IReadOnlyList<KeyValuePair<string, TypedConstant>>? namedArguments )
        {
            this.Constructor = constructor;
            this.ConstructorArguments = constructorArguments?.ToImmutableArray() ?? ImmutableArray<TypedConstant>.Empty;
            this.NamedArguments = new NamedArgumentList( namedArguments );
        }

        /// <summary>
        /// Creates a new <see cref="AttributeConstruction"/> by explicitly specifying the constructor and strongly-typed arguments.
        /// </summary>
        /// <param name="constructor">The attribute constructor.</param>
        /// <param name="constructorArguments">The constructor arguments.</param>
        /// <param name="namedArguments">The named arguments (i.e., the assigned fields and properties).</param>
        /// <returns>A new <see cref="AttributeConstruction"/> instance.</returns>
        public static AttributeConstruction Create(
            IConstructor constructor,
            IReadOnlyList<TypedConstant>? constructorArguments = default,
            IReadOnlyList<KeyValuePair<string, TypedConstant>>? namedArguments = default )
            => new(
                constructor,
                constructorArguments,
                namedArguments );

        /// <summary>
        /// Creates a new <see cref="AttributeConstruction"/> by specifying the reflection <see cref="System.Type"/> of the attribute.
        /// The method will attempt to find a suitable constructor.
        /// </summary>
        /// <param name="attributeType">The attribute type.</param>
        /// <param name="constructorArguments">The constructor arguments.</param>
        /// <param name="namedArguments">The named arguments (i.e., the assigned fields and properties).</param>
        /// <returns>A new <see cref="AttributeConstruction"/> instance.</returns>
        public static AttributeConstruction Create(
            Type attributeType,
            IReadOnlyList<object?>? constructorArguments = null,
            IReadOnlyList<KeyValuePair<string, object?>>? namedArguments = null )
            => Create(
                TypeFactory.GetNamedType( attributeType ),
                constructorArguments,
                namedArguments );

        /// <summary>
        /// Creates a new <see cref="AttributeConstruction"/> by specifying the <see cref="INamedType"/> of the attribute.
        /// The method will attempt to find a suitable constructor.
        /// </summary>
        /// <param name="attributeType">The attribute type.</param>
        /// <param name="constructorArguments">The constructor arguments.</param>
        /// <param name="namedArguments">The named arguments (i.e., the assigned fields and properties).</param>
        /// <returns>A new <see cref="AttributeConstruction"/> instance.</returns>
        public static AttributeConstruction Create(
            INamedType attributeType,
            IReadOnlyList<object?>? constructorArguments = null,
            IReadOnlyList<KeyValuePair<string, object?>>? namedArguments = null )
        {
            constructorArguments ??= ImmutableArray<object?>.Empty;
            namedArguments ??= ImmutableArray<KeyValuePair<string, object?>>.Empty;

            // Translate IType and System.Type arguments to System.Type to get the correct constructor.
            // This handles IType implementations, CompileTimeType, RuntimeType, and other Type subclasses.
            var constructorArgumentTypes =
                constructorArguments
                    .Select( x => x?.GetType() )
                    .Select(
                        x => x == null ? null :
                            typeof(IType).IsAssignableFrom( x ) || typeof(Type).IsAssignableFrom( x ) ? typeof(Type) :
                            x )
                    .ToArray();

            var constructors = attributeType.Constructors.OfCompatibleSignature( constructorArgumentTypes ).ToList();

            switch ( constructors.Count )
            {
                case 0:
                    throw new ArgumentOutOfRangeException( nameof(constructorArguments), "Cannot find a constructor that is compatible with these arguments." );

                case > 1:
                    throw new ArgumentOutOfRangeException( nameof(constructorArguments), "Found more than one constructor compatible with these arguments." );
            }

            var constructor = constructors[0];

            // Map constructor arguments.
            var typedConstructorArguments = ImmutableArray.CreateBuilder<TypedConstant>( constructor.Parameters.Count );
            var isLastParameterParams = constructor.Parameters.Count > 0 && constructor.Parameters[^1].IsParams;

            for ( var i = 0; i < constructor.Parameters.Count; i++ )
            {
                var parameterType = constructor.Parameters[i].Type;

                if ( isLastParameterParams && i == constructor.Parameters.Count - 1 )
                {
                    // The current parameter is `params`.
                    var arrayType = (IArrayType) parameterType;
                    var paramsParameterValues = new List<TypedConstant>();

                    if ( constructorArguments.Count == constructor.Parameters.Count
                         && TypedConstant.CheckAcceptableType(
                             parameterType,
                             constructorArguments[i],
                             false,
                             ((ICompilationInternal) attributeType.Compilation).Factory ) )
                    {
                        var constructorArgument = constructorArguments[i];

                        // An array is passed to the `params` parameter.
                        if ( constructorArgument != null )
                        {
                            foreach ( var arrayItem in (IEnumerable) constructorArgument )
                            {
                                paramsParameterValues.Add( TypedConstant.UnwrapOrCreate( arrayItem, arrayType.ElementType ) );
                            }

                            typedConstructorArguments.Add( TypedConstant.UnwrapOrCreate( paramsParameterValues.ToImmutableArray(), parameterType ) );
                        }
                        else
                        {
                            // Null is explicitly passed for the params array parameter.
                            typedConstructorArguments.Add( TypedConstant.UnwrapOrCreate( null, parameterType ) );
                        }
                    }
                    else
                    {
                        // A list is passed to the `params` parameter. Transform this into an array.

                        for ( var j = i; j < constructorArguments.Count; j++ )
                        {
                            paramsParameterValues.Add( TypedConstant.UnwrapOrCreate( constructorArguments[j], arrayType.ElementType ) );
                        }

                        typedConstructorArguments.Add( TypedConstant.UnwrapOrCreate( paramsParameterValues.ToImmutableArray(), parameterType ) );
                    }
                }
                else
                {
                    typedConstructorArguments.Add( TypedConstant.UnwrapOrCreate( constructorArguments[i], parameterType ) );
                }
            }

            // Map named arguments.
            var typedNamedArguments = ImmutableArray.CreateBuilder<KeyValuePair<string, TypedConstant>>( namedArguments.Count );

            foreach ( var argument in namedArguments )
            {
                // TODO: inherited members
                var name = argument.Key;

                var fieldOrProperty = constructor.DeclaringType.FieldsAndProperties.OfName( name ).SingleOrDefault() ?? throw new ArgumentOutOfRangeException(
                    nameof(namedArguments),
                    $"The type '{constructor.DeclaringType.ToDisplayString( CodeDisplayFormat.ShortDiagnosticMessage )}' does not contain a field or property named '{name}'." );

                typedNamedArguments.Add(
                    new KeyValuePair<string, TypedConstant>( argument.Key, TypedConstant.UnwrapOrCreate( argument.Value, fieldOrProperty.Type ) ) );
            }

            return new AttributeConstruction( constructor, typedConstructorArguments.MoveToImmutable(), typedNamedArguments.MoveToImmutable() );
        }
    }
}