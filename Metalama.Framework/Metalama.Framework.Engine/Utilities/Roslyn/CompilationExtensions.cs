// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Caching;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

public static class CompilationExtensions
{
    private static readonly WeakCache<Compilation, ImmutableDictionary<string, SyntaxTree>> _indexedSyntaxTreesCache = new();

    public static ImmutableDictionary<string, SyntaxTree> GetIndexedSyntaxTrees( this Compilation compilation )
        => _indexedSyntaxTreesCache.GetOrAdd( compilation, GetIndexedSyntaxTreesCore );

    private static ImmutableDictionary<string, SyntaxTree> GetIndexedSyntaxTreesCore( Compilation compilation )
        => compilation.SyntaxTrees.ToImmutableDictionary( x => x.FilePath, x => x );

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
                _ => CSharpParseOptions.Default,
            },
            path,
            Encoding.UTF8 );
}