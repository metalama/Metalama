// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
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
        /// <remarks>
        /// A <see cref="HashSet{T}"/> and not an immutable hash set, because this set is read on a hot
        /// path -- once per named type of the compilation -- and an immutable hash set pays a tree walk per lookup.
        /// It is built once and never mutated afterwards, and the field that holds it is private and read-only.
        /// <c>IReadOnlySet{T}</c> would state that better but does not exist in <c>netstandard2.0</c>, which this
        /// assembly targets.
        /// </remarks>
        public static HashSet<string> ReadTypeNameList( AnalyzerConfigOptions options, string key )
        {
            var result = new HashSet<string>( StringComparer.Ordinal );

            if ( !options.TryGetValue( key, out var value ) || string.IsNullOrWhiteSpace( value ) )
            {
                return result;
            }

            foreach ( var name in value.Split( ';' ) )
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
        /// Determines whether a property is automatically implemented, that is, whether it has an accessor list in
        /// which no accessor has a body.
        /// </summary>
        /// <remarks>
        /// <c>DeclarationExtensions.IsAutoProperty</c> in <c>Metalama.Framework.Engine</c> answers the same question
        /// over the same Roslyn symbol, and this is deliberately shaped like its <c>HasExplicitAccessorBody</c>. It
        /// cannot be reused: this assembly references only Roslyn, because it ships to customers and must carry the
        /// smallest possible closure. The engine's version also classifies the C# 13 semi-automatic property, which
        /// matters there and not here, because a semi-automatic property has a backing field and is therefore already
        /// covered by the loop over fields; this method is only the safety net for the shapes where Roslyn exposes no
        /// backing field.
        /// </remarks>
        public static bool IsAutomaticallyImplemented( IPropertySymbol property )
        {
            // An abstract or extern property has no backing field and no body to inspect.
            if ( property.IsAbstract || property.IsExtern || property.DeclaringSyntaxReferences.IsDefaultOrEmpty )
            {
                return false;
            }

            foreach ( var reference in property.DeclaringSyntaxReferences )
            {
                switch ( reference.GetSyntax() )
                {
                    // An expression-bodied property computes its value and holds no state of its own.
                    case PropertyDeclarationSyntax { ExpressionBody: not null }:
                        return false;

                    case BasePropertyDeclarationSyntax { AccessorList.Accessors: { Count: > 0 } accessors }:
                        foreach ( var accessor in accessors )
                        {
                            if ( accessor.Body != null || accessor.ExpressionBody != null )
                            {
                                return false;
                            }
                        }

                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Computes which type parameters of a generic definition appear in the type of one of its instance fields,
        /// and which a construction of that type must therefore supply as satisfying whatever contract the definition
        /// carries.
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
        public static StoredTypeParameters ComputeStoredTypeParameters( INamedTypeSymbol definition, int maxDepth )
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
                var result = definition.TypeParameters.Length >= StoredTypeParameters.MaxOrdinal ? ulong.MaxValue : (1UL << definition.TypeParameters.Length) - 1;

                // A contravariant parameter is dropped, and this is a heuristic rather than a proof. Contravariance
                // puts T in input position, which means an implementation does receive a T and could retain it:
                // ISink<in T>.Accept( T value ) is free to store what it is given. What contravariance does say is
                // that such an interface is written to consume, and in this codebase they do consume:
                // IEligibilityRule<in T> and IAnnotation<in T> evaluate their argument and keep nothing. Requiring
                // the argument anyway reported IEligibilityRule<IDeclaration> and every rule built on it, which was
                // noise rather than a finding. The exchange is deliberate, and it is unsound in the direction of
                // silence: an implementation that does retain its input is not reported here.
                for ( var i = 0; i < definition.TypeParameters.Length && i < StoredTypeParameters.MaxOrdinal; i++ )
                {
                    if ( definition.TypeParameters[i].Variance == VarianceKind.In )
                    {
                        result &= ~(1UL << i);
                    }
                }

                return new StoredTypeParameters( result );
            }
            else
            {
                ulong result = 0;

                // The fields of the type itself, and then those it inherits. A type parameter stored by a base class
                // is stored by the derived one just as surely: for Derived<T> : Base<T> where Base<T> holds a T,
                // Derived<Compilation> holds a Compilation, and reading only the fields declared here would accept it.
                for ( var current = definition; current != null; current = current.BaseType?.OriginalDefinition )
                {
                    foreach ( var member in current.GetMembers() )
                    {
                        if ( member is IFieldSymbol { IsStatic: false, IsConst: false } field )
                        {
                            // The field type is written in terms of the base type's own parameters, so it is mapped
                            // back to the parameters of the definition being asked about before the bits are set.
                            CollectTypeParameters(
                                MapToDefinition( field.Type, definition, current ),
                                definition,
                                ref result,
                                0,
                                maxDepth );
                        }
                    }
                }

                return new StoredTypeParameters( result );
            }
        }

        /// <summary>
        /// Substitutes the type arguments that a definition passes to one of its base definitions, so that a field
        /// declared on the base is read in terms of the parameters of the definition being asked about.
        /// </summary>
        /// <remarks>
        /// For <c>Derived{T} : Base{T}</c> the field <c>Base{T}._value</c> is typed with the <c>T</c> of
        /// <c>Base</c>. Walking from <c>Derived</c> up to <c>Base</c> and substituting at each step turns it into the
        /// <c>T</c> of <c>Derived</c>, which is the parameter whose bit has to be set. When the substitution cannot
        /// be performed, the field type is returned unchanged and its parameters simply do not match the owner, which
        /// loses the bit rather than setting a wrong one.
        /// </remarks>
        private static ITypeSymbol MapToDefinition( ITypeSymbol fieldType, INamedTypeSymbol definition, INamedTypeSymbol declaringDefinition )
        {
            if ( SymbolEqualityComparer.Default.Equals( definition, declaringDefinition ) )
            {
                return fieldType;
            }

            // Find the constructed base that leads from the definition to the declaring one, and read the field type
            // through its type arguments.
            for ( var baseType = definition.BaseType; baseType != null; baseType = baseType.BaseType )
            {
                if ( SymbolEqualityComparer.Default.Equals( baseType.OriginalDefinition, declaringDefinition ) )
                {
                    return Substitute( fieldType, declaringDefinition, baseType );
                }
            }

            return fieldType;
        }

        private static ITypeSymbol Substitute( ITypeSymbol type, INamedTypeSymbol definition, INamedTypeSymbol constructed )
        {
            if ( type is ITypeParameterSymbol typeParameter
                 && SymbolEqualityComparer.Default.Equals( typeParameter.ContainingType?.OriginalDefinition, definition )
                 && typeParameter.Ordinal < constructed.TypeArguments.Length )
            {
                return constructed.TypeArguments[typeParameter.Ordinal];
            }

            if ( type is INamedTypeSymbol { IsGenericType: true } namedType )
            {
                var arguments = new ITypeSymbol[namedType.TypeArguments.Length];
                var changed = false;

                for ( var i = 0; i < arguments.Length; i++ )
                {
                    arguments[i] = Substitute( namedType.TypeArguments[i], definition, constructed );
                    changed |= !SymbolEqualityComparer.Default.Equals( arguments[i], namedType.TypeArguments[i] );
                }

                if ( changed )
                {
                    return namedType.OriginalDefinition.Construct( arguments );
                }
            }

            return type;
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
                    when typeParameter.Ordinal < StoredTypeParameters.MaxOrdinal
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
