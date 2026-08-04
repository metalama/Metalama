// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Utilities.Caching;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

[PublicAPI]
public static class CompilationExtensions
{
    private static readonly WeakCache<Compilation, SyntaxTreeIndex> _syntaxTreeIndexCache = new( isStaticCache: true );

    internal static SyntaxTreeIndex GetSyntaxTreeIndex( this Compilation compilation )
        => _syntaxTreeIndexCache.GetOrAdd( compilation, SyntaxTreeIndex.Create );

    /// <summary>
    /// Gets the syntax trees of a <see cref="Compilation"/> indexed by <see cref="DocumentKey"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When two syntax trees represent one document, the first one in the order of
    /// <see cref="Compilation.SyntaxTrees"/> is indexed and the others are not. See <see cref="SyntaxTreeIndex"/> for
    /// why that condition is reachable and why it is resolved rather than reported here. A caller that goes on to
    /// process the indexed trees must first remove the others from the compilation, which
    /// <see cref="RemoveDuplicatePathSyntaxTrees"/> does.
    /// </para>
    /// <para>
    /// The dictionary is read-only rather than immutable, because it is built once per compilation and never updated.
    /// </para>
    /// </remarks>
    public static IReadOnlyDictionary<DocumentKey, SyntaxTree> GetIndexedSyntaxTrees( this Compilation compilation )
        => compilation.GetSyntaxTreeIndex().SyntaxTreesByDocumentKey;

    /// <summary>
    /// Gets the syntax trees of a <see cref="Compilation"/> that share their <see cref="SyntaxTree.FilePath"/> with an
    /// earlier tree, and are therefore excluded from <see cref="GetIndexedSyntaxTrees"/>. Empty for every compilation
    /// produced by the command-line compiler, which deduplicates source files itself.
    /// </summary>
    public static ImmutableArray<SyntaxTree> GetDuplicatePathSyntaxTrees( this Compilation compilation )
        => compilation.GetSyntaxTreeIndex().DuplicatePathSyntaxTrees;

    /// <summary>
    /// Removes from a <see cref="Compilation"/> every syntax tree that shares its path with an earlier one, so that a
    /// path identifies a syntax tree of the result. Returns the argument unchanged, without allocating, when no path is
    /// duplicated, which is the ordinary case.
    /// </summary>
    /// <remarks>
    /// This is the single point at which Metalama resolves the condition. Everything downstream may then assume that
    /// the index of <see cref="GetIndexedSyntaxTrees"/> covers the compilation it describes.
    /// </remarks>
    public static Compilation RemoveDuplicatePathSyntaxTrees( this Compilation compilation )
    {
        var index = compilation.GetSyntaxTreeIndex();

        return index.HasDuplicatePaths ? compilation.RemoveSyntaxTrees( index.DuplicatePathSyntaxTrees ) : compilation;
    }

    internal static INamespaceSymbol? GetDescendant( this INamespaceSymbol parentNamespace, string ns )
    {
        var namespaceCursor = parentNamespace;

        if ( ns == "" )
        {
            return namespaceCursor;
        }

        foreach ( var part in ns.Split( '.' ) )
        {
            namespaceCursor = namespaceCursor.GetMembers( part ).OfType<INamespaceSymbol>().SingleOrDefault();

            if ( namespaceCursor == null )
            {
                return null;
            }
        }

        return namespaceCursor;
    }

    public static SemanticModel GetCachedSemanticModel( this Compilation compilation, SyntaxTree syntaxTree, bool ignoreAccessibility = false )
        => SemanticModelProvider.GetInstance( compilation ).GetSemanticModel( syntaxTree, ignoreAccessibility );

    public static SemanticModelProvider GetSemanticModelProvider( this Compilation compilation ) => SemanticModelProvider.GetInstance( compilation );

    public static Compilation RewriteAll( this Compilation compilation, Func<Compilation, SyntaxTree, CSharpSyntaxRewriter> getRewriter )
    {
        var modifiedCompilation = compilation;

        foreach ( var tree in compilation.SyntaxTrees )
        {
            var rewriter = getRewriter( compilation, tree );
            var root = tree.GetRoot();
            var rewrittenRoot = rewriter.Visit( root );

            if ( root != rewrittenRoot )
            {
                modifiedCompilation = modifiedCompilation.ReplaceSyntaxTree( tree, tree.WithRootAndOptions( rewrittenRoot, tree.Options ) );
            }
        }

        return modifiedCompilation;
    }

    internal static LanguageVersion GetLanguageVersion( this Compilation compilation )
    {
        var tree = compilation.SyntaxTrees.FirstOrDefault();

        if ( tree == null )
        {
            return LanguageVersion.Default.MapSpecifiedToEffectiveVersion();
        }

        return ((CSharpParseOptions) tree.Options).LanguageVersion;
    }

    internal static SyntaxTree CreateEmptySyntaxTree( this Compilation compilation, string path )
        => CSharpSyntaxTree.Create(
            SyntaxFactory.CompilationUnit(),
            compilation.SyntaxTrees.FirstOrDefault() switch
            {
                { Options: CSharpParseOptions options } => options,
                _ => CSharpParseOptions.Default
            },
            path,
            Encoding.UTF8 );
}