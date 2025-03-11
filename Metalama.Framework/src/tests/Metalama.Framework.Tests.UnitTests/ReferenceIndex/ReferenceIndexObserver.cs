// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.ReferenceGraph;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Tests.UnitTests.ReferenceIndex;

internal sealed class ReferenceIndexObserver : IReferenceIndexObserver
{
    private readonly ConcurrentQueue<ISymbol> _resolvedSymbols = new();
    private readonly ConcurrentQueue<SemanticModel> _resolvedSemanticModels = new();

    public IReadOnlyList<string> ResolvedSymbolNames
        => this._resolvedSymbols.SelectAsReadOnlyCollection( s => s.ToTestName() ).OrderBy( x => x ).Distinct().ToReadOnlyList();

    public IReadOnlyCollection<string> ResolvedSemanticModelNames
        => this._resolvedSemanticModels.SelectAsReadOnlyCollection( m => m.SyntaxTree.FilePath ).OrderBy( x => x ).ToReadOnlyList();

    public void OnSymbolResolved( ISymbol symbol )
    {
        this._resolvedSymbols.Enqueue( symbol );
    }

    public void OnSemanticModelResolved( SemanticModel semanticModel )
    {
        this._resolvedSemanticModels.Enqueue( semanticModel );
    }
}