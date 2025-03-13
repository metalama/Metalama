// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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