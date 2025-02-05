// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Linking.Inlining;

internal sealed class InliningAnalysisInfo
{
    public SyntaxNode ReplacedRootNode { get; }

    public string? ReturnVariableIdentifier { get; }

    public InliningAnalysisInfo( SyntaxNode replacedRootNode, string? returnVariableIdentifier )
    {
        this.ReplacedRootNode = replacedRootNode;
        this.ReturnVariableIdentifier = returnVariableIdentifier;
    }
}