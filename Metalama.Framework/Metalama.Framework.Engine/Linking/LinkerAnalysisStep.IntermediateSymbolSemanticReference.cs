// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerAnalysisStep
{
    public record struct IntermediateSymbolSemanticReference(
        IntermediateSymbolSemantic<IMethodSymbol> ContainingSemantic,
        IntermediateSymbolSemantic TargetSemantic,
        SyntaxNode ReferencingNode );
}