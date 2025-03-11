// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Caching;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

public sealed class SemanticModelProvider
{
    private static readonly WeakCache<Compilation, SemanticModelProvider> _instances = new();
    private readonly Compilation _compilation;
    private readonly ConcurrentDictionary<SyntaxTree, Cached> _semanticModels = new();

    private SemanticModelProvider( Compilation compilation )
    {
        this._compilation = compilation;
    }

    internal static SemanticModelProvider GetInstance( Compilation compilation ) => _instances.GetOrAdd( compilation, c => new SemanticModelProvider( c ) );

    public SemanticModel GetSemanticModel( SyntaxTree syntaxTree, bool ignoreAccessibility = false )
    {
        var node = this._semanticModels.GetOrAdd( syntaxTree, _ => new Cached() );

        if ( ignoreAccessibility )
        {
            node.IgnoringAccessibility ??= this._compilation.GetSemanticModel( syntaxTree, true );

            return node.IgnoringAccessibility;
        }
        else
        {
            node.Default ??= this._compilation.GetSemanticModel( syntaxTree );

            return node.Default;
        }
    }

    private sealed class Cached
    {
        public SemanticModel? Default { get; set; }

        public SemanticModel? IgnoringAccessibility { get; set; }
    }
}