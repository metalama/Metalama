// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.ReferenceGraph;

internal interface IReferenceIndexObserver : IProjectService
{
    void OnSymbolResolved( ISymbol symbol );

    void OnSemanticModelResolved( SemanticModel semanticModel );
}