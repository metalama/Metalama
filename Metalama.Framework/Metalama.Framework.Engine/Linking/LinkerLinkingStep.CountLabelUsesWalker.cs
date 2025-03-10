// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerLinkingStep
{
    // TODO: This is temporary for unneeded label removal until the linker uses control flow analysis results for inlining.
    private sealed class CountLabelUsesWalker : SafeSyntaxWalker
    {
        public Dictionary<string, int> ObservedLabelCounters { get; } = new();

        public override void VisitLocalFunctionStatement( LocalFunctionStatementSyntax node )
        {
            // Don't visit local functions (may have same labels).
        }

        public override void VisitGotoStatement( GotoStatementSyntax node )
        {
            if ( node.Expression is IdentifierNameSyntax identifierName )
            {
                this.ObservedLabelCounters.TryGetValue( identifierName.Identifier.ValueText, out var counter );
                this.ObservedLabelCounters[identifierName.Identifier.ValueText] = counter + 1;
            }
        }
    }
}