// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerAnalysisStep
{
    private struct ForcefullyInitializedType
    {
        public IReadOnlyList<IntermediateSymbolSemantic<IMethodSymbol>> Constructors { get; }

        public IReadOnlyList<ISymbol> InitializedSymbols { get; }

        public ForcefullyInitializedType( IReadOnlyList<IntermediateSymbolSemantic<IMethodSymbol>> constructors, IReadOnlyList<ISymbol> initializedSymbols )
        {
            this.Constructors = constructors;
            this.InitializedSymbols = initializedSymbols;
        }
    }
}