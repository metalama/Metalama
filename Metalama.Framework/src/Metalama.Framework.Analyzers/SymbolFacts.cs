// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Text;

namespace Metalama.Framework.Analyzers
{
    /// <summary>
    /// The facts about symbols that both contracts of this assembly need, and that carry no policy of their own.
    /// </summary>
    /// <remarks>
    /// The <c>Durable</c> and the <c>ImmutableType</c> contracts answer different questions and disagree on several
    /// types, so they keep separate classifiers and separate tables. What they share is the plumbing: how a type is
    /// named in a message, how a name is written in a table, how a semicolon-separated build property is read, and
    /// which type parameters of a generic definition are actually stored. That is what lives here.
    /// </remarks>
    internal static class SymbolFacts
    {
        /// <summary>
        /// The format of a type name in a diagnostic message.
        /// </summary>
        /// <remarks>
        /// The nullable annotation is omitted deliberately. Neither contract depends on it, and the verdict caches are
        /// keyed by <see cref="SymbolEqualityComparer.Default"/>, which does not distinguish <c>string</c> from
        /// <c>string?</c>. A message that mentioned the annotation could therefore name the annotation of whichever of
        /// the two happened to be evaluated first.
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
        /// Returns the full metadata name of a type, that is, the name by which the tables and the MSBuild items refer
        /// to it: the namespace, the chain of containing types separated by <c>+</c>, and the name of the type with
        /// its arity.
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
        /// Reads one of the semicolon-separated lists that an MSBuild item is joined into by the build, because an
        /// analyzer can read a property but not an item.
        /// </summary>
        public static ImmutableHashSet<string> ReadTypeNameList( AnalyzerConfigOptions options, string key )
        {
            if ( !options.TryGetValue( key, out var value ) || string.IsNullOrWhiteSpace( value ) )
            {
                return ImmutableHashSet<string>.Empty;
            }

            var builder = ImmutableHashSet.CreateBuilder( StringComparer.Ordinal );

            foreach ( var name in value.Split( ';' ) )
            {
                var trimmed = name.Trim();

                if ( trimmed.Length > 0 )
                {
                    builder.Add( trimmed );
                }
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Determines whether a property is automatically implemented, that is, whether its accessors have no body.
        /// </summary>
        public static bool IsAutomaticallyImplemented( IPropertySymbol property )
        {
            var accessor = property.GetMethod ?? property.SetMethod;

            if ( accessor == null || accessor.DeclaringSyntaxReferences.IsDefaultOrEmpty )
            {
                return false;
            }

            foreach ( var reference in accessor.DeclaringSyntaxReferences )
            {
                if ( reference.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.AccessorDeclarationSyntax
                    { Body: null, ExpressionBody: null } )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns, as a bit per ordinal, the type parameters of a generic definition that appear in the type of one
        /// of its instance fields, and which a construction of that type must therefore supply as satisfying whatever
        /// contract the definition carries.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Requiring every type argument to satisfy the contract would be simpler and is wrong, because the first
        /// exception would be the most important durable type of the codebase: <c>IDurableRef{T}</c> stores a
        /// serializable identifier and never a <c>T</c>, so its type argument is a phantom. Requiring only the
        /// parameters that are actually stored drops that case with no special rule.
        /// </para>
        /// <para>
        /// An interface or an abstract type has no fields to examine, so every parameter is assumed stored.
        /// </para>
        /// </remarks>
        public static ulong ComputeStoredTypeParameters( INamedTypeSymbol definition, int maxDepth )
        {
            // An interface or an abstract type has no fields to examine.
            //
            // A type that comes from another assembly has none that can be seen either: a compilation is created with
            // MetadataImportOptions.Public by default, so Roslyn does not expose the private fields of a referenced
            // assembly at all. Reading the empty field list as "this type stores nothing" would silently exempt every
            // generic type outside the compilation, DurableLazy<T> first among them. Assuming instead that every
            // parameter is stored is the conservative answer, and the remedy for a metadata type that genuinely has a
            // phantom parameter is an entry in one of the well-known tables or in an MSBuild item.
            if ( definition.TypeKind == TypeKind.Interface
                 || definition.IsAbstract
                 || definition.DeclaringSyntaxReferences.IsDefaultOrEmpty )
            {
                var result = definition.TypeParameters.Length >= 64 ? ulong.MaxValue : (1UL << definition.TypeParameters.Length) - 1;

                // A contravariant parameter appears only in input position, so an implementation cannot store a value
                // of that type: it never receives one to store. Without this, IEligibilityRule<in T> and
                // IAnnotation<in T> would demand that their argument satisfy the contract, and
                // IEligibilityRule<IDeclaration> would be reported although the rule stores no declaration.
                for ( var i = 0; i < definition.TypeParameters.Length && i < 64; i++ )
                {
                    if ( definition.TypeParameters[i].Variance == VarianceKind.In )
                    {
                        result &= ~(1UL << i);
                    }
                }

                return result;
            }
            else
            {
                ulong result = 0;

                foreach ( var member in definition.GetMembers() )
                {
                    if ( member is IFieldSymbol { IsStatic: false, IsConst: false } field )
                    {
                        CollectTypeParameters( field.Type, definition, ref result, 0, maxDepth );
                    }
                }

                return result;
            }
        }

        private static void CollectTypeParameters( ITypeSymbol type, INamedTypeSymbol owner, ref ulong result, int depth, int maxDepth )
        {
            if ( depth > maxDepth )
            {
                return;
            }

            switch ( type )
            {
                case ITypeParameterSymbol typeParameter
                    when typeParameter.Ordinal < 64
                         && SymbolEqualityComparer.Default.Equals( typeParameter.ContainingType?.OriginalDefinition, owner ):
                    result |= 1UL << typeParameter.Ordinal;

                    break;

                case IArrayTypeSymbol array:
                    CollectTypeParameters( array.ElementType, owner, ref result, depth + 1, maxDepth );

                    break;

                case INamedTypeSymbol { IsGenericType: true } namedType:
                    foreach ( var argument in namedType.TypeArguments )
                    {
                        CollectTypeParameters( argument, owner, ref result, depth + 1, maxDepth );
                    }

                    break;
            }
        }
    }
}
