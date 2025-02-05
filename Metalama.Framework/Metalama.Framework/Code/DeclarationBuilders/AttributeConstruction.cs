// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
    /// Encapsulates the information necessary to create a custom attribute. 
    /// </summary>
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
        public static AttributeConstruction Create(
            Type attributeType,
            IReadOnlyList<object?>? constructorArguments = null,
            IReadOnlyList<KeyValuePair<string, object?>>? namedArguments = null )
            => Create(
                (INamedType) TypeFactory.GetType( attributeType ),
                constructorArguments,
                namedArguments );

        /// <summary>
        /// Creates a new <see cref="AttributeConstruction"/> by specifying the <see cref="INamedType"/> of the attribute.
        /// The method will attempt to find a suitable constructor.
        /// </summary>
        public static AttributeConstruction Create(
            INamedType attributeType,
            IReadOnlyList<object?>? constructorArguments = null,
            IReadOnlyList<KeyValuePair<string, object?>>? namedArguments = null )
        {
            constructorArguments ??= ImmutableArray<object?>.Empty;
            namedArguments ??= ImmutableArray<KeyValuePair<string, object?>>.Empty;

            // Translate provided IType - typed parameters to System.Type to get the correct constructor.
            // Also translate CompileTimeType to System.Type, since CompileTimeType can't be translated to a symbol in the run-time assembly.
            var constructorArgumentTypes =
                constructorArguments
                    .Select( x => x?.GetType() )
                    .Select(
                        x => x == null ? null :
                            typeof(IType).IsAssignableFrom( x ) || x.FullName == "Metalama.Framework.Engine.ReflectionMocks.CompileTimeType" ? typeof(Type) :
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
                        }
                    }
                    else
                    {
                        // A list is passed to the `params` parameter. Transform this into an array.

                        for ( var j = i; j < constructorArguments.Count; j++ )
                        {
                            paramsParameterValues.Add( TypedConstant.UnwrapOrCreate( constructorArguments[j], arrayType.ElementType ) );
                        }
                    }

                    typedConstructorArguments.Add( TypedConstant.UnwrapOrCreate( paramsParameterValues.ToImmutableArray(), parameterType ) );
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