// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.DeclarationBuilders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Provides extension methods to work with generic declarations.
    /// </summary>
    [CompileTime]
    [PublicAPI]
    public static class GenericExtensions
    {
        /// <summary>
        /// Returns <c>true</c> if the current declaration, or any the declaring type, is generic.
        /// </summary>
        public static bool IsSelfOrDeclaringTypeGeneric( this IMemberOrNamedType declaration )
            => declaration is IGeneric { IsGeneric: true } || (declaration.DeclaringType != null && declaration.DeclaringType.IsSelfOrDeclaringTypeGeneric());

        /// <summary>
        /// Gets the base type of a type or the base member of an overridden member, if any.
        /// </summary>
        public static IMemberOrNamedType? GetBase( this IMemberOrNamedType declaration )
            => declaration switch
            {
                INamedType namedType => namedType.BaseType,
                IMethod method => method.OverriddenMethod,
                IProperty property => property.OverriddenProperty,
                IEvent @event => @event.OverriddenEvent,
                IIndexer indexer => indexer.OverriddenIndexer,
                _ => null
            };

        [Obsolete( "Use the Definition property." )]
        public static IDeclaration GetOriginalDefinition( this IDeclaration declaration ) => declaration.GetDefinition();

        internal static IDeclaration GetDefinition( this IDeclaration declaration )
            => declaration switch
            {
                IMemberOrNamedType memberOrNamedType => memberOrNamedType.Definition,
                _ => declaration
            };

        [Obsolete( "Use the Definition property." )]
        public static INamedType GetOriginalDefinition( this INamedType declaration ) => declaration.Definition;

        [Obsolete( "Use the Definition property." )]
        public static IMemberOrNamedType GetOriginalDefinition( this IMemberOrNamedType declaration ) => declaration.Definition;

        [Obsolete( "Use the Definition property." )]
        public static IMember GetOriginalDefinition( this IMember declaration ) => declaration.Definition;

        [Obsolete( "Use the Definition property." )]
        public static IMethod GetOriginalDefinition( this IMethod declaration ) => declaration.Definition;

        [Obsolete( "Use the Definition property." )]
        public static IProperty GetOriginalDefinition( this IProperty declaration ) => declaration.Definition;

        [Obsolete( "Use the Definition property." )]
        public static IEvent GetOriginalDefinition( this IEvent declaration ) => declaration.Definition;

        [Obsolete( "Use the Definition property." )]
        public static IConstructor GetOriginalDefinition( this IConstructor declaration ) => declaration.Definition;

        /// <summary>
        /// Constructs a generic instance of an <see cref="INamedType"/>, with type arguments given as <see cref="IType"/>.
        /// </summary>
        public static INamedType WithTypeArguments( this INamedType type, params IType[] typeArguments ) => type.MakeGenericInstance( typeArguments );

        private static IReadOnlyList<IType> ConvertTypes( ICompilation compilation, IReadOnlyList<Type> types )
        {
            var convertedTypes = new IType[types.Count];

            for ( var i = 0; i < convertedTypes.Length; i++ )
            {
                convertedTypes[i] = compilation.Factory.GetTypeByReflectionType( types[i] );
            }

            return convertedTypes;
        }

        [Obsolete( "Use MakeGenericInstance instead." )]
        public static INamedType WithTypeArguments( this INamedType type, params Type[] typeArguments ) => type.MakeGenericInstance( typeArguments );

        public static INamedType MakeGenericInstance( this INamedType type, params Type[] typeArguments )
            => type.MakeGenericInstance( ConvertTypes( type.Compilation, typeArguments ) );

        public static INamedType MakeGenericInstance( this INamedType type, params IType[] typeArguments ) => type.MakeGenericInstance( typeArguments );

        [Obsolete( "Use MakeGenericInstance instead." )]
        public static INamedType WithTypeArguments( this INamedType type, IReadOnlyList<Type> typeArguments ) => type.MakeGenericInstance( typeArguments );

        public static INamedType MakeGenericInstance( this INamedType type, IReadOnlyList<Type> typeArguments )
            => type.MakeGenericInstance( ConvertTypes( type.Compilation, typeArguments ) );

        [Obsolete( "Use INamedType.MakeGenericInstance instead." )]
        public static INamedType WithTypeArguments( this INamedType type, IReadOnlyList<IType> typeArguments ) => type.MakeGenericInstance( typeArguments );

        [Obsolete( "Use MakeGenericInstance instead." )]
        public static IMethod WithTypeArguments( this IMethod method, params Type[] typeArguments )
            => method.MakeGenericInstance( ConvertTypes( method.Compilation, typeArguments ) );

        [Obsolete( "Use MakeGenericInstance instead." )]
        public static IMethod WithTypeArguments( this IMethod method, params IType[] typeArguments ) => method.MakeGenericInstance( typeArguments );

        [Obsolete( "Use MakeGenericInstance instead." )]
        public static IMethod MakeGenericInstance( this IMethod method, IReadOnlyList<Type> typeArguments )
            => method.MakeGenericInstance( ConvertTypes( method.Compilation, typeArguments ) );

        public static IMethod MakeGenericInstance( this IMethod method, params Type[] typeArguments )
            => method.MakeGenericInstance( ConvertTypes( method.Compilation, typeArguments ) );

        public static IMethod MakeGenericInstance( this IMethod method, params IType[] typeArguments ) => method.MakeGenericInstance( typeArguments );

        public static IMethod WithTypeArguments( this IMethod method, IReadOnlyList<IType> typeArguments ) => method.MakeGenericInstance( typeArguments );

        [Obsolete(
            "This method does not handle nested generic types into account. Use INamedType.MakeGenericInstance for each type, then IMethod.MakeGenericInstance." )]
        public static IMethod WithTypeArguments( this IMethod method, IReadOnlyList<Type> typeTypeArguments, IReadOnlyList<Type> methodTypeArguments )
            => method.ForTypeInstance( method.DeclaringType.MakeGenericInstance( typeTypeArguments ) ).MakeGenericInstance( methodTypeArguments );

        [Obsolete(
            "This method does not handle nested generic types into account. Use INamedType.MakeGenericInstance for each type, then IMethod.MakeGenericInstance." )]
        public static IMethod WithTypeArguments( this IMethod method, Type[] typeTypeArguments, Type[] methodTypeArguments )
            => method.ForTypeInstance( method.DeclaringType.MakeGenericInstance( typeTypeArguments ) ).WithTypeArguments( methodTypeArguments );

        public static IMemberOrNamedType ForTypeInstance( this IMemberOrNamedType declaration, INamedType typeInstance )
            => ForTypeInstanceImpl( declaration, typeInstance );

        /// <summary>
        /// Returns a representation of the current nested <see cref="INamedType"/>, but for a different generic instance
        /// of the declaring type.
        /// </summary>
        public static INamedType ForTypeInstance( this INamedType declaration, INamedType typeInstance )
            => (INamedType) ForTypeInstanceImpl( declaration, typeInstance );

        /// <summary>
        /// Returns a representation of the current <see cref="IField"/>, but for a different generic instance
        /// of the declaring type.
        /// </summary>
        public static IField ForTypeInstance( this IField declaration, INamedType typeInstance ) => (IField) ForTypeInstanceImpl( declaration, typeInstance );

        /// <summary>
        /// Returns a representation of the current <see cref="IMethod"/>, but for a different generic instance
        /// of the declaring type.
        /// </summary>
        public static IMethod ForTypeInstance( this IMethod declaration, INamedType typeInstance )
            => (IMethod) ForTypeInstanceImpl( declaration, typeInstance );

        /// <summary>
        /// Returns a representation of the current <see cref="IProperty"/>, but for a different generic instance
        /// of the declaring type.
        /// </summary>
        public static IProperty ForTypeInstance( this IProperty declaration, INamedType typeInstance )
            => (IProperty) ForTypeInstanceImpl( declaration, typeInstance );

        /// <summary>
        /// Returns a representation of the current <see cref="IEvent"/>, but for a different generic instance
        /// of the declaring type.
        /// </summary>
        public static IEvent ForTypeInstance( this IEvent declaration, INamedType typeInstance ) => (IEvent) ForTypeInstanceImpl( declaration, typeInstance );

        /// <summary>
        /// Returns a representation of the current <see cref="IConstructor"/>, but for a different generic instance
        /// of the declaring type.
        /// </summary>
        public static IConstructor ForTypeInstance( this IConstructor declaration, INamedType typeInstance )
            => (IConstructor) ForTypeInstanceImpl( declaration, typeInstance );

        private static IMemberOrNamedType ForTypeInstanceImpl( this IMemberOrNamedType declaration, INamedType typeInstance )
        {
            if ( declaration.DeclarationKind == DeclarationKind.Field )
            {
                var field = (IField) declaration;

                if ( field.OverridingProperty != null )
                {
                    var propertyImpl = (IProperty) ForTypeInstanceCanonical( field.OverridingProperty, typeInstance );

                    return propertyImpl.OriginalField!;
                }
            }

            return ForTypeInstanceCanonical( declaration, typeInstance );
        }

        private static IMemberOrNamedType ForTypeInstanceCanonical( IMemberOrNamedType declaration, INamedType typeInstance )
        {
            if ( declaration.DeclaringType == null )
            {
                throw new InvalidOperationException( $"The type '{declaration.ToDisplayString()}' is not a type member or nested type." );
            }

            var thisOriginalDeclaration = declaration.Definition;

            if ( !typeInstance.Definition.Equals( thisOriginalDeclaration.DeclaringType! ) )
            {
                throw new ArgumentOutOfRangeException(
                    nameof(typeInstance),
                    $"The type must be identical to or constructed from '{thisOriginalDeclaration.DeclaringType!.ToDisplayString()}'." );
            }

            if ( declaration is IDeclarationBuilder )
            {
                throw new ArgumentOutOfRangeException( nameof(declaration), "The declaration must not be an IDeclarationBuilder." );
            }

            if ( !declaration.DeclaringType.IsSelfOrDeclaringTypeGeneric() )
            {
                return declaration;
            }

            IEnumerable<IMemberOrNamedType> candidates;

            // TODO PERF: Implement the switch based on DeclarationKind. 

            switch ( declaration )
            {
                case INamedType namedType:
                    candidates = typeInstance.Types.OfName( namedType.Name );

                    break;

                case IMethod method:
                    candidates = typeInstance.Methods.OfName( method.Name );

                    break;

                case IField { OverridingProperty: null } field:
                    candidates = typeInstance.Fields.OfName( field.Name );

                    break;

                case IProperty property:
                    candidates = typeInstance.Properties.OfName( property.Name );

                    break;

                case IEvent @event:
                    candidates = typeInstance.Events.OfName( @event.Name );

                    break;

                case IConstructor { IsStatic: false }:
                    candidates = typeInstance.Constructors;

                    break;

                case IConstructor { IsStatic: true }:
                    candidates = typeInstance.StaticConstructor != null ? new[] { typeInstance.StaticConstructor } : Array.Empty<IMemberOrNamedType>();

                    break;

                default:
                    throw new ArgumentOutOfRangeException( nameof(declaration) );
            }

            return candidates.Single( c => c.Definition.Equals( thisOriginalDeclaration ) );
        }
    }
}