// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.References;
using Microsoft.CodeAnalysis;
using System;
using SpecialType = Microsoft.CodeAnalysis.SpecialType;

namespace Metalama.Framework.Engine.CodeModel
{
    /// <summary>
    /// Provides extension methods to bridge between the Metalama code model (<see cref="IDeclaration"/>, <see cref="IType"/>, etc.)
    /// and the Roslyn symbol model (<see cref="ISymbol"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class enables aspect weavers to convert Metalama declarations to Roslyn symbols for use with the Roslyn API.
    /// It also provides methods to go from Roslyn symbols back to Metalama declarations.
    /// </para>
    /// <para>
    /// Common operations:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="GetSymbol(ICompilationElement, bool)"/>: Get the Roslyn <see cref="ISymbol"/> for a Metalama declaration.</description></item>
    /// <item><description><see cref="GetDeclaration(ICompilation, ISymbol)"/>: Get the Metalama <see cref="IDeclaration"/> for a Roslyn symbol.</description></item>
    /// <item><description><see cref="GetRoslynCompilation(ICompilation)"/>: Get the underlying Roslyn <see cref="Compilation"/>.</description></item>
    /// <item><description><see cref="GetSemanticModel(ICompilation, SyntaxTree)"/>: Get a <see cref="SemanticModel"/> for a syntax tree.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso href="@roslyn-api"/>
    [PublicAPI]
    public static class SymbolExtensions
    {
        private static ISymbol? GetSymbolImpl( ICompilationElement declaration, bool returnNullIfMappingRequired )
            => declaration switch
            {
                ISymbolBasedCompilationElement { SymbolMustBeMapped: false, Symbol: { } symbol } => symbol,
                ISymbolBasedCompilationElement when !returnNullIfMappingRequired => throw new ArgumentOutOfRangeException(
                    nameof(declaration),
                    $"The symbol of '{declaration}' is available, but it must be mapped with the generic context" ),
                _ => null // not symbol-backed.
            };

        /// <summary>
        /// Gets the Roslyn <see cref="ISymbol"/> for a Metalama declaration.
        /// </summary>
        /// <param name="declaration">The Metalama declaration.</param>
        /// <param name="returnNullIfMappingRequired">If <c>true</c>, returns <c>null</c> when the symbol requires generic context mapping;
        /// if <c>false</c>, throws <see cref="ArgumentOutOfRangeException"/>.</param>
        /// <returns>The Roslyn symbol, or <c>null</c> if not symbol-backed or mapping is required.</returns>
        public static ISymbol? GetSymbol( this ICompilationElement declaration, bool returnNullIfMappingRequired = true )
            => GetSymbolImpl( declaration, returnNullIfMappingRequired );

        /// <summary>
        /// Gets the Roslyn <see cref="ISymbol"/> for a declaration reference in a specific compilation.
        /// </summary>
        /// <param name="declaration">The declaration reference.</param>
        /// <param name="compilation">The Roslyn compilation to resolve the symbol in.</param>
        /// <param name="ignoreAssemblyKey">If <c>true</c>, ignores assembly key when resolving.</param>
        /// <returns>The Roslyn symbol, or <c>null</c> if not found.</returns>
        public static ISymbol? GetSymbol( this IRef declaration, Compilation compilation, bool ignoreAssemblyKey = false )
            => ((ISdkRef) declaration).GetSymbol( compilation, ignoreAssemblyKey );

        private static T? GetSymbol<T>( this ICompilationElement declaration, bool returnNullIfMappingRequired = true )
            where T : class, ISymbol
            => (T?) GetSymbolImpl( declaration, returnNullIfMappingRequired );

        /// <summary>
        /// Gets the Roslyn <see cref="ITypeSymbol"/> for a Metalama <see cref="IType"/>.
        /// </summary>
        /// <param name="type">The Metalama type.</param>
        /// <param name="returnNullIfMappingRequired">If <c>true</c>, returns <c>null</c> when the symbol requires generic context mapping.</param>
        /// <returns>The Roslyn type symbol, or <c>null</c> if not available.</returns>
        public static ITypeSymbol? GetSymbol( this IType type, bool returnNullIfMappingRequired = true )
            => type.GetSymbol<ITypeSymbol>( returnNullIfMappingRequired );

        /// <summary>
        /// Gets the Roslyn <see cref="INamedTypeSymbol"/> for a Metalama <see cref="INamedType"/>.
        /// </summary>
        /// <param name="namedType">The Metalama named type.</param>
        /// <param name="returnNullIfMappingRequired">If <c>true</c>, returns <c>null</c> when the symbol requires generic context mapping.</param>
        /// <returns>The Roslyn named type symbol, or <c>null</c> if not available.</returns>
        public static INamedTypeSymbol? GetSymbol( this INamedType namedType, bool returnNullIfMappingRequired = true )
            => namedType.GetSymbol<INamedTypeSymbol>( returnNullIfMappingRequired );

        /// <summary>
        /// Gets the Roslyn <see cref="ITypeParameterSymbol"/> for a Metalama <see cref="ITypeParameter"/>.
        /// </summary>
        /// <param name="typeParameter">The Metalama type parameter.</param>
        /// <param name="returnNullIfMappingRequired">If <c>true</c>, returns <c>null</c> when the symbol requires generic context mapping.</param>
        /// <returns>The Roslyn type parameter symbol, or <c>null</c> if not available.</returns>
        public static ITypeParameterSymbol? GetSymbol( this ITypeParameter typeParameter, bool returnNullIfMappingRequired = true )
            => typeParameter.GetSymbol<ITypeParameterSymbol>( returnNullIfMappingRequired );

        /// <summary>
        /// Gets the Roslyn <see cref="IMethodSymbol"/> for a Metalama <see cref="IMethodBase"/> (method or constructor).
        /// </summary>
        /// <param name="method">The Metalama method or constructor.</param>
        /// <param name="returnNullIfMappingRequired">If <c>true</c>, returns <c>null</c> when the symbol requires generic context mapping.</param>
        /// <returns>The Roslyn method symbol, or <c>null</c> if not available.</returns>
        public static IMethodSymbol? GetSymbol( this IMethodBase method, bool returnNullIfMappingRequired = true )
            => method.GetSymbol<IMethodSymbol>( returnNullIfMappingRequired );

        /// <summary>
        /// Gets the Roslyn <see cref="IPropertySymbol"/> for a Metalama <see cref="IProperty"/>.
        /// </summary>
        /// <param name="property">The Metalama property.</param>
        /// <param name="returnNullIfMappingRequired">If <c>true</c>, returns <c>null</c> when the symbol requires generic context mapping.</param>
        /// <returns>The Roslyn property symbol, or <c>null</c> if not available.</returns>
        public static IPropertySymbol? GetSymbol( this IProperty property, bool returnNullIfMappingRequired = true )
            => property.GetSymbol<IPropertySymbol>( returnNullIfMappingRequired );

        /// <summary>
        /// Gets the Roslyn <see cref="IEventSymbol"/> for a Metalama <see cref="IEvent"/>.
        /// </summary>
        /// <param name="event">The Metalama event.</param>
        /// <param name="returnNullIfMappingRequired">If <c>true</c>, returns <c>null</c> when the symbol requires generic context mapping.</param>
        /// <returns>The Roslyn event symbol, or <c>null</c> if not available.</returns>
        public static IEventSymbol? GetSymbol( this IEvent @event, bool returnNullIfMappingRequired = true )
            => @event.GetSymbol<IEventSymbol>( returnNullIfMappingRequired );

        /// <summary>
        /// Gets the Roslyn <see cref="IFieldSymbol"/> for a Metalama <see cref="IField"/>.
        /// </summary>
        /// <param name="field">The Metalama field.</param>
        /// <param name="returnNullIfMappingRequired">If <c>true</c>, returns <c>null</c> when the symbol requires generic context mapping.</param>
        /// <returns>The Roslyn field symbol, or <c>null</c> if not available.</returns>
        public static IFieldSymbol? GetSymbol( this IField field, bool returnNullIfMappingRequired = true )
            => field.GetSymbol<IFieldSymbol>( returnNullIfMappingRequired );

        /// <summary>
        /// Gets the Roslyn <see cref="IParameterSymbol"/> for a Metalama <see cref="IParameter"/>.
        /// </summary>
        /// <param name="parameter">The Metalama parameter.</param>
        /// <param name="returnNullIfMappingRequired">If <c>true</c>, returns <c>null</c> when the symbol requires generic context mapping.</param>
        /// <returns>The Roslyn parameter symbol, or <c>null</c> if not available.</returns>
        public static IParameterSymbol? GetSymbol( this IParameter parameter, bool returnNullIfMappingRequired = true )
            => parameter.GetSymbol<IParameterSymbol>( returnNullIfMappingRequired );

        /// <summary>
        /// Gets the Roslyn <see cref="IAssemblySymbol"/> for a Metalama <see cref="IAssembly"/>.
        /// </summary>
        /// <param name="assembly">The Metalama assembly.</param>
        /// <param name="returnNullIfMappingRequired">If <c>true</c>, returns <c>null</c> when the symbol requires generic context mapping.</param>
        /// <returns>The Roslyn assembly symbol.</returns>
        public static IAssemblySymbol GetSymbol( this IAssembly assembly, bool returnNullIfMappingRequired = true )
            => assembly.GetSymbol<IAssemblySymbol>( returnNullIfMappingRequired );

        /// <summary>
        /// Gets the member that the specified symbol overrides, if any.
        /// </summary>
        /// <param name="symbol">The Roslyn symbol (method, property, or event).</param>
        /// <param name="returnNullIfMappingRequired">Unused, for API consistency.</param>
        /// <returns>The overridden member, or <c>null</c> if none or not applicable.</returns>
        public static ISymbol? GetOverriddenMember( this ISymbol? symbol, bool returnNullIfMappingRequired = true )
            => symbol switch
            {
                IMethodSymbol method => method.OverriddenMethod,
                IPropertySymbol property => property.OverriddenProperty,
                IEventSymbol @event => @event.OverriddenEvent,
                _ => null
            };

        /// <summary>
        /// Gets the type of the expression represented by the symbol (e.g., the return type for a method, the field type for a field).
        /// </summary>
        /// <param name="symbol">The Roslyn symbol.</param>
        /// <returns>The expression type, or <c>null</c> if <c>void</c> or not applicable.</returns>
        public static ITypeSymbol? GetExpressionType( this ISymbol symbol )
        {
            var type = ExpressionTypeVisitor.Instance.Visit( symbol );

            if ( type is { SpecialType: SpecialType.System_Void } )
            {
                return null;
            }
            else
            {
                return type;
            }
        }

        /// <summary>
        /// Gets the underlying Roslyn <see cref="Compilation"/> from a Metalama <see cref="ICompilation"/>.
        /// </summary>
        public static Compilation GetRoslynCompilation( this ICompilation compilation ) => ((ISdkCompilation) compilation).RoslynCompilation;

        /// <summary>
        /// Gets a cached <see cref="SemanticModel"/> for a syntax tree in the compilation.
        /// </summary>
        /// <param name="compilation">The Metalama compilation.</param>
        /// <param name="syntaxTree">The syntax tree.</param>
        /// <returns>The semantic model for the syntax tree.</returns>
        public static SemanticModel GetSemanticModel( this ICompilation compilation, SyntaxTree syntaxTree )
            => ((ISdkCompilation) compilation).GetCachedSemanticModel( syntaxTree );

        /// <summary>
        /// Attempts to get the Metalama <see cref="IDeclaration"/> for a Roslyn <see cref="ISymbol"/>.
        /// </summary>
        /// <param name="compilation">The Metalama compilation.</param>
        /// <param name="symbol">The Roslyn symbol.</param>
        /// <param name="declaration">When successful, the Metalama declaration.</param>
        /// <returns><c>true</c> if the symbol was found in the compilation; otherwise <c>false</c>.</returns>
        public static bool TryGetDeclaration( this ICompilation compilation, ISymbol symbol, out IDeclaration? declaration )
            => ((ISdkCompilation) compilation).Factory.TryGetDeclaration( symbol, out declaration );

        /// <summary>
        /// Gets the Metalama <see cref="IDeclaration"/> for a Roslyn <see cref="ISymbol"/>.
        /// </summary>
        /// <param name="compilation">The Metalama compilation.</param>
        /// <param name="symbol">The Roslyn symbol.</param>
        /// <returns>The Metalama declaration.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The symbol is not found in the compilation.</exception>
        public static IDeclaration GetDeclaration( this ICompilation compilation, ISymbol symbol )
        {
            if ( !compilation.TryGetDeclaration( symbol, out var declaration ) )
            {
                throw new ArgumentOutOfRangeException( nameof(symbol), $"The symbol '{symbol}' cannot be mapped to the compilation '{compilation}'." );
            }

            return declaration;
        }

        /// <summary>
        /// Gets the root definition of a symbol (the original definition before any generic substitution).
        /// </summary>
        /// <param name="symbol">The Roslyn symbol.</param>
        /// <returns>The original definition of the symbol.</returns>
        public static ISymbol GetRootDefinition( this ISymbol symbol )
            => symbol.Kind switch
            {
                SymbolKind.Field => ((IFieldSymbol) symbol).GetRootDefinition(),
                _ => symbol.OriginalDefinition
            };

        /// <summary>
        /// Gets the root definition of a field symbol, handling tuple fields specially.
        /// </summary>
        /// <param name="symbol">The field symbol.</param>
        /// <returns>The original definition of the field.</returns>
        public static IFieldSymbol GetRootDefinition( this IFieldSymbol symbol )
            => symbol.CorrespondingTupleField.Equals( symbol )
                ? symbol.OriginalDefinition
                : symbol.CorrespondingTupleField?.OriginalDefinition ?? symbol.OriginalDefinition;

        /// <summary>
        /// Determines whether a symbol is its own definition, using equality comparison rather than identity.
        /// </summary>
        /// <param name="symbol">The Roslyn symbol.</param>
        /// <returns><c>true</c> if the symbol is a definition; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// This method differs from <see cref="ISymbol.IsDefinition"/> in that it uses equality comparison,
        /// making it tolerant to non-identical but equal symbol instances. Tuple types are always considered definitions.
        /// </remarks>
        public static bool IsDefinitionSafe( this ISymbol symbol )
            => symbol.Equals( symbol.OriginalDefinition ) || symbol is INamedTypeSymbol { IsTupleType: true };

        private sealed class ExpressionTypeVisitor : SymbolVisitor<ITypeSymbol>
        {
            public static ExpressionTypeVisitor Instance { get; } = new();

            public override ITypeSymbol VisitEvent( IEventSymbol symbol ) => symbol.Type;

            public override ITypeSymbol VisitField( IFieldSymbol symbol ) => symbol.Type;

            public override ITypeSymbol VisitLocal( ILocalSymbol symbol ) => symbol.Type;

            public override ITypeSymbol VisitMethod( IMethodSymbol symbol ) => symbol.ReturnType;

            public override ITypeSymbol VisitParameter( IParameterSymbol symbol ) => symbol.Type;

            public override ITypeSymbol VisitProperty( IPropertySymbol symbol ) => symbol.Type;

            public override ITypeSymbol? DefaultVisit( ISymbol symbol ) => null;
        }
    }
}