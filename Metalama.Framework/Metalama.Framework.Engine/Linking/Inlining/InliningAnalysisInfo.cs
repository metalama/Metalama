// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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