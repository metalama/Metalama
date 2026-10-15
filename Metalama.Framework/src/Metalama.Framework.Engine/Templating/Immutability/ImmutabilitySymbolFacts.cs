// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Options;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Metalama.Framework.Engine.Templating.Immutability
{
    /// <summary>
    /// The facts about symbols that the immutability rules need and that carry no policy of their own.
    /// </summary>
    internal static class ImmutabilitySymbolFacts
    {
        /// <summary>
        /// The format of a type name in a diagnostic message.
        /// </summary>
        /// <remarks>
        /// The nullable annotation is omitted. Immutability does not depend on it, and the verdict cache is keyed by
        /// <see cref="SymbolEqualityComparer.Default"/>, which does not distinguish <c>string</c> from <c>string?</c>.
        /// A message that mentioned the annotation could therefore name the annotation of whichever of the two was
        /// evaluated first.
        /// </remarks>
        private static readonly SymbolDisplayFormat _displayFormat =
            SymbolDisplayFormat.MinimallyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayFormat.MinimallyQualifiedFormat.MiscellaneousOptions
                & ~SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier );

        /// <summary>
        /// Returns the name of a type as it appears in a diagnostic message.
        /// </summary>
        public static string GetDisplayName( ITypeSymbol type ) => type.ToDisplayString( _displayFormat );

        /// <summary>
        /// Returns the location of the application of an attribute, or <c>null</c> when the attribute is not in source.
        /// </summary>
        public static Location? GetApplicationLocation( AttributeData attribute )
            => attribute.ApplicationSyntaxReference is { } reference
                ? Location.Create( reference.SyntaxTree, reference.Span )
                : null;

        /// <summary>
        /// Determines whether a field or property can be assigned by code outside the type that declares it.
        /// </summary>
        /// <remarks>
        /// A rule that judges a member by the values assigned to it is sound only where every assignment is visible.
        /// Private write access confines every assignment to the declaring type, including a nested type and another part
        /// of a partial declaration. Any wider access allows code in another assembly to write the member. An
        /// <c>init</c> accessor counts as writable here, and the immutability rules accept it before they reach this
        /// predicate.
        /// </remarks>
        public static bool IsWritableFromOutsideDeclaringType( ISymbol member )
            => member.Kind switch
            {
                SymbolKind.Field when member is IFieldSymbol field
                    => !field.IsConst && !field.IsReadOnly && field.DeclaredAccessibility != Accessibility.Private,
                SymbolKind.Property when member is IPropertySymbol { SetMethod: { } setMethod }
                    => setMethod.DeclaredAccessibility != Accessibility.Private,
                _ => false
            };

        /// <summary>
        /// Returns the full metadata name of a type, that is, the name by which the tables and the MSBuild items refer to
        /// it: the namespace, the chain of containing types separated by <c>+</c>, and the name of the type with its
        /// arity.
        /// </summary>
        public static string GetFullMetadataName( INamedTypeSymbol type )
        {
            var builder = new StringBuilder();
            AppendFullMetadataName( builder, type );

            return builder.ToString();
        }

        private static void AppendFullMetadataName( StringBuilder builder, INamedTypeSymbol type )
        {
            if ( type.ContainingType != null )
            {
                AppendFullMetadataName( builder, type.ContainingType );
                builder.Append( '+' );
            }
            else if ( type.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace )
            {
                builder.Append( containingNamespace.ToDisplayString() );
                builder.Append( '.' );
            }

            builder.Append( type.MetadataName );
        }

        /// <summary>
        /// Reads one of the semicolon-separated lists into which the build joins an MSBuild item.
        /// </summary>
        public static HashSet<string> ReadTypeNameList( IProjectOptions? options, string propertyName )
        {
            var result = new HashSet<string>( StringComparer.Ordinal );

            if ( options == null || !options.TryGetProperty( propertyName, out var value ) || string.IsNullOrWhiteSpace( value ) )
            {
                return result;
            }

            foreach ( var name in value!.Split( ';' ) )
            {
                var trimmed = name.Trim();

                if ( trimmed.Length > 0 )
                {
                    result.Add( trimmed );
                }
            }

            return result;
        }

        /// <summary>
        /// Computes which type parameters of a generic definition appear in the type of one of its instance fields, and
        /// which a construction of that type must therefore supply as immutable.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Requiring every type argument to be immutable would reject a type whose type parameter is a phantom, such as
        /// <c>IDurableRef{T}</c>, which stores an identifier and never a <c>T</c>.
        /// </para>
        /// <para>
        /// An interface, an abstract type and a type of another assembly have no fields that can be examined, so every
        /// parameter of such a type is assumed to be stored, except a contravariant one. A contravariant parameter is in
        /// input position, and the interfaces of this kind in Metalama, such as <c>IEligibilityRule{T}</c>, evaluate
        /// their argument and do not store it. This is a heuristic: an implementation that stores its input is not
        /// reported.
        /// </para>
        /// </remarks>
        public static StoredTypeParameters ComputeStoredTypeParameters( INamedTypeSymbol definition, int maxDepth )
        {
            if ( definition.TypeKind == TypeKind.Interface
                 || definition.IsAbstract
                 || definition.DeclaringSyntaxReferences.IsDefaultOrEmpty )
            {
                var bits = definition.TypeParameters.Length >= StoredTypeParameters.MaxOrdinal
                    ? ulong.MaxValue
                    : (1UL << definition.TypeParameters.Length) - 1;

                for ( var i = 0; i < definition.TypeParameters.Length && i < StoredTypeParameters.MaxOrdinal; i++ )
                {
                    if ( definition.TypeParameters[i].Variance == VarianceKind.In )
                    {
                        bits &= ~(1UL << i);
                    }
                }

                return new StoredTypeParameters( bits );
            }

            ulong result = 0;

            // The fields of the type itself, and then the fields it inherits. A type parameter stored by a base class is
            // also stored by the derived class.
            for ( var current = definition; current != null; current = current.BaseType?.OriginalDefinition )
            {
                var constructedBase = FindConstructedBase( definition, current );

                foreach ( var member in current.GetMembers() )
                {
                    if ( member.Kind == SymbolKind.Field && member is IFieldSymbol { IsStatic: false, IsConst: false } field )
                    {
                        CollectTypeParameters( field.Type, definition, current, constructedBase, ref result, 0, maxDepth );
                    }
                }
            }

            return new StoredTypeParameters( result );
        }

        /// <summary>
        /// Returns the base type, as the definition constructs it, that leads from the definition to the type that
        /// declares a field, or <c>null</c> when the field is declared by the definition itself.
        /// </summary>
        private static INamedTypeSymbol? FindConstructedBase( INamedTypeSymbol definition, INamedTypeSymbol declaringDefinition )
        {
            if ( SymbolEqualityComparer.Default.Equals( definition, declaringDefinition ) )
            {
                return null;
            }

            for ( var baseType = definition.BaseType; baseType != null; baseType = baseType.BaseType )
            {
                if ( SymbolEqualityComparer.Default.Equals( baseType.OriginalDefinition, declaringDefinition ) )
                {
                    return baseType;
                }
            }

            return null;
        }

        private static void CollectTypeParameters(
            ITypeSymbol type,
            INamedTypeSymbol owner,
            INamedTypeSymbol declaringDefinition,
            INamedTypeSymbol? constructedBase,
            ref ulong result,
            int depth,
            int maxDepth )
        {
            if ( depth > maxDepth )
            {
                return;
            }

            switch ( type.Kind )
            {
                case SymbolKind.TypeParameter when type is ITypeParameterSymbol typeParameter
                                                   && SymbolEqualityComparer.Default.Equals( typeParameter.ContainingType?.OriginalDefinition, owner ):
                    if ( typeParameter.Ordinal < StoredTypeParameters.MaxOrdinal )
                    {
                        result |= 1UL << typeParameter.Ordinal;
                    }

                    break;

                case SymbolKind.TypeParameter when type is ITypeParameterSymbol typeParameter
                                                   && constructedBase != null
                         && SymbolEqualityComparer.Default.Equals( typeParameter.ContainingType?.OriginalDefinition, declaringDefinition )
                         && typeParameter.Ordinal < constructedBase.TypeArguments.Length:
                    CollectTypeParameters(
                        constructedBase.TypeArguments[typeParameter.Ordinal],
                        owner,
                        declaringDefinition,
                        constructedBase,
                        ref result,
                        depth + 1,
                        maxDepth );

                    break;

                case SymbolKind.ArrayType when type is IArrayTypeSymbol array:
                    CollectTypeParameters( array.ElementType, owner, declaringDefinition, constructedBase, ref result, depth + 1, maxDepth );

                    break;

                case SymbolKind.NamedType when type is INamedTypeSymbol { IsGenericType: true } namedType:
                    foreach ( var argument in namedType.TypeArguments )
                    {
                        CollectTypeParameters( argument, owner, declaringDefinition, constructedBase, ref result, depth + 1, maxDepth );
                    }

                    break;
            }
        }
    }

    /// <summary>
    /// The set of type parameters of a generic definition that are stored in one of its instance fields, addressed by
    /// ordinal.
    /// </summary>
    internal readonly struct StoredTypeParameters
    {
        /// <summary>
        /// The number of ordinals that a <see cref="ulong"/> can address. A parameter at or beyond this ordinal is
        /// treated as stored.
        /// </summary>
        public const int MaxOrdinal = 64;

        private readonly ulong _bits;

        public StoredTypeParameters( ulong bits )
        {
            this._bits = bits;
        }

        /// <summary>
        /// Determines whether the type parameter at an ordinal is stored, and therefore whether the type argument
        /// supplied for it must be immutable.
        /// </summary>
        public bool IsStored( int ordinal ) => ordinal >= MaxOrdinal || (this._bits & (1UL << ordinal)) != 0;
    }
}
