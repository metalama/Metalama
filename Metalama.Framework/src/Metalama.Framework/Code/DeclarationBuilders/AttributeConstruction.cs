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
    /// The <see cref="Create(Type, IReadOnlyList{object}, IReadOnlyList{KeyValuePair{string, object}})"/> and
    /// <see cref="Create(INamedType, IReadOnlyList{object}, IReadOnlyList{KeyValuePair{string, object}})"/> overloads
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
        /// Creates a new <see cref="AttributeConstruction"/> by explicitly specifying the constructor and loosely-typed arguments.
        /// The arguments are automatically converted to <see cref="TypedConstant"/> based on the constructor parameter types.
        /// </summary>
        /// <param name="constructor">The attribute constructor.</param>
        /// <param name="constructorArguments">The constructor arguments.</param>
        /// <param name="namedArguments">The named arguments (i.e., the assigned fields and properties).</param>
        /// <returns>A new <see cref="AttributeConstruction"/> instance.</returns>
        public static AttributeConstruction Create(
            IConstructor constructor,
            IReadOnlyList<object?>? constructorArguments,
            IReadOnlyList<KeyValuePair<string, object?>>? namedArguments = null )
        {
            constructorArguments ??= ImmutableArray<object?>.Empty;
            namedArguments ??= ImmutableArray<KeyValuePair<string, object?>>.Empty;

            // Map constructor arguments.
            var typedConstructorArguments = MapConstructorArguments( constructor, constructorArguments );

            // Map named arguments.
            var typedNamedArguments = MapNamedArguments( constructor, namedArguments );

            return new AttributeConstruction( constructor, typedConstructorArguments, typedNamedArguments );
        }

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

            // Determine argument types for constructor resolution.
            // For TypedConstant arguments, use their declared Type (not the runtime type of Value,
            // which would be incorrect for enums where Value is the underlying integer type).
            // For IType/Type arguments, map to System.Type (attribute constructor parameter type).
            var constructorArgumentTypes = new IType?[constructorArguments.Count];

            for ( var i = 0; i < constructorArguments.Count; i++ )
            {
                var arg = constructorArguments[i];

                constructorArgumentTypes[i] = arg switch
                {
                    TypedConstant tc => tc.Type,
                    null => null,
                    IType or System.Type => TypeFactory.GetType( typeof(Type) ),
                    _ => TypeFactory.GetType( arg.GetType() )
                };
            }

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
            var typedConstructorArguments = MapConstructorArguments( constructor, constructorArguments );

            // Map named arguments.
            var typedNamedArguments = MapNamedArguments( constructor, namedArguments );

            return new AttributeConstruction( constructor, typedConstructorArguments, typedNamedArguments );
        }

        private static ImmutableArray<TypedConstant> MapConstructorArguments(
            IConstructor constructor,
            IReadOnlyList<object?> constructorArguments )
        {
            var typedConstructorArguments = ImmutableArray.CreateBuilder<TypedConstant>( constructor.Parameters.Count );
            var isLastParameterParams = constructor.Parameters.Count > 0 && constructor.Parameters[^1].IsParams;

            // Validate argument count.
            if ( isLastParameterParams )
            {
                if ( constructorArguments.Count < constructor.Parameters.Count - 1 )
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(constructorArguments),
                        $"Too few arguments: expected at least {constructor.Parameters.Count - 1} but got {constructorArguments.Count}." );
                }
            }
            else
            {
                if ( constructorArguments.Count != constructor.Parameters.Count )
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(constructorArguments),
                        $"Wrong number of arguments: expected {constructor.Parameters.Count} but got {constructorArguments.Count}." );
                }
            }

            for ( var i = 0; i < constructor.Parameters.Count; i++ )
            {
                var parameterType = constructor.Parameters[i].Type;

                if ( isLastParameterParams && i == constructor.Parameters.Count - 1 )
                {
                    // The current parameter is `params`.
                    var arrayType = (IArrayType) parameterType;

                    if ( constructorArguments.Count <= i )
                    {
                        // No arguments provided for the params parameter — create an empty array.
                        typedConstructorArguments.Add( TypedConstant.UnwrapOrCreate( ImmutableArray<TypedConstant>.Empty, parameterType ) );
                    }
                    else
                    {
                        var rawArgument = constructorArguments[i];

                        if ( rawArgument is TypedConstant tc && constructorArguments.Count == constructor.Parameters.Count )
                        {
                            // A TypedConstant is passed directly for the params parameter — use it as-is.
                            typedConstructorArguments.Add( tc );
                        }
                        else
                        {
                            var paramsParameterValues = new List<TypedConstant>();

                            // Unwrap TypedConstant to get the underlying value for type checking.
                            var constructorArgument = rawArgument is TypedConstant tcToUnwrap ? tcToUnwrap.Value : rawArgument;

                            if ( constructorArguments.Count == constructor.Parameters.Count
                                 && TypedConstant.CheckAcceptableType(
                                     parameterType,
                                     constructorArgument,
                                     false,
                                     ((ICompilationInternal) constructor.DeclaringType.Compilation).Factory ) )
                            {
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
                    }
                }
                else
                {
                    typedConstructorArguments.Add( TypedConstant.UnwrapOrCreate( constructorArguments[i], parameterType ) );
                }
            }

            return typedConstructorArguments.MoveToImmutable();
        }

        private static ImmutableArray<KeyValuePair<string, TypedConstant>> MapNamedArguments(
            IConstructor constructor,
            IReadOnlyList<KeyValuePair<string, object?>> namedArguments )
        {
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

            return typedNamedArguments.MoveToImmutable();
        }
    }
}