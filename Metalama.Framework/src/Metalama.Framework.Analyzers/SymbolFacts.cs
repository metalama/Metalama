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
        /// Returns the location of the application of an attribute, so that a diagnostic about the attribute itself
        /// lands on the attribute rather than on the declaration that carries it.
        /// </summary>
        /// <remarks>
        /// Returns <c>null</c> for an attribute that is not in source, which is the case for every type of a
        /// referenced assembly. A rule whose only remedy is to delete a line of source has nothing to say about such a
        /// type. The location is built from the syntax reference rather than from <c>GetSyntax</c>, which would parse
        /// the tree to produce a node that is then only asked for its span.
        /// </remarks>
        public static Location? GetApplicationLocation( AttributeData attribute )
            => attribute.ApplicationSyntaxReference is { } reference
                ? Location.Create( reference.SyntaxTree, reference.Span )
                : null;

        /// <summary>
        /// Determines whether a field or property can be assigned by code outside the type that declares it, and
        /// therefore outside this compilation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Both contracts need this, and for the same reason: a rule that judges a member by the values assigned to it
        /// is sound only where the analyzer sees every assignment. Private write access confines every assignment to
        /// the declaring type, including a nested type and another part of a partial declaration. Anything wider,
        /// whether internal, protected or public, can be written by code the analyzer never sees.
        /// </para>
        /// <para>
        /// An <c>init</c> accessor counts as writable here, which is where the two contracts part company. It confines
        /// an assignment to construction, which is all that immutability asks, but the object initializer that
        /// performs it may sit in any assembly, so it does not confine an assignment to anywhere the analyzer can
        /// look. The immutability rules therefore accept <c>init</c> before they reach this predicate, and the
        /// durability rules do not.
        /// </para>
        /// </remarks>
        public static bool IsWritableFromOutsideDeclaringType( ISymbol member )
            => member switch
            {
                IFieldSymbol { IsConst: true } => false,
                IFieldSymbol field => !field.IsReadOnly && field.DeclaredAccessibility != Accessibility.Private,
                IPropertySymbol { SetMethod: { } setMethod } => setMethod.DeclaredAccessibility != Accessibility.Private,
                IPropertySymbol => false,
                _ => false
            };

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
                    // The base as the definition constructs it, which is what maps a parameter of the base to a
                    // parameter of the definition. Null when the fields being read are the definition's own.
                    var constructedBase = FindConstructedBase( definition, current );

                    foreach ( var member in current.GetMembers() )
                    {
                        if ( member is IFieldSymbol { IsStatic: false, IsConst: false } field )
                        {
                            CollectTypeParameters( field.Type, definition, current, constructedBase, ref result, 0, maxDepth );
                        }
                    }
                }

                return new StoredTypeParameters( result );
            }
        }

        /// <summary>
        /// Returns the base type, as the definition constructs it, that leads from the definition to the one
        /// declaring a field, or <c>null</c> when the field is declared by the definition itself.
        /// </summary>
        /// <remarks>
        /// For <c>Derived{T} : Base{T}</c> a field declared on <c>Base</c> is typed with the <c>T</c> of
        /// <c>Base</c>, and the bit to set is the one of the <c>T</c> of <c>Derived</c>. The constructed base
        /// <c>Base{T}</c> carries that correspondence: the argument at the ordinal of the base parameter is the
        /// derived parameter.
        /// </remarks>
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

        /// <remarks>
        /// A parameter of the definition being asked about sets its bit. A parameter of the base that declares the
        /// field is read through <paramref name="constructedBase"/> and followed, which is what makes an inherited
        /// field count and what makes it count through an array or a nested generic type.
        /// </remarks>
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

            switch ( type )
            {
                case ITypeParameterSymbol typeParameter
                    when SymbolEqualityComparer.Default.Equals( typeParameter.ContainingType?.OriginalDefinition, owner ):
                    if ( typeParameter.Ordinal < StoredTypeParameters.MaxOrdinal )
                    {
                        result |= 1UL << typeParameter.Ordinal;
                    }

                    break;

                case ITypeParameterSymbol typeParameter
                    when constructedBase != null
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

                case IArrayTypeSymbol array:
                    CollectTypeParameters( array.ElementType, owner, declaringDefinition, constructedBase, ref result, depth + 1, maxDepth );

                    break;

                case INamedTypeSymbol { IsGenericType: true } namedType:
                    foreach ( var argument in namedType.TypeArguments )
                    {
                        CollectTypeParameters( argument, owner, declaringDefinition, constructedBase, ref result, depth + 1, maxDepth );
                    }

                    break;
            }
        }
    }
}
