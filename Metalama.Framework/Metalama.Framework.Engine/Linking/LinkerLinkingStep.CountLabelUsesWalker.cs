// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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