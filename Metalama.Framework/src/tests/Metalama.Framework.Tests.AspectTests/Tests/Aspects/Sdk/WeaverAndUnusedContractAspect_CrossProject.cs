// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Threading.Tasks;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// This test covers issue #2226. A referenced project declares a contract aspect, and the current project
// uses a weaver aspect but does not apply the contract aspect. This is the situation of a project that
// references Metalama.Patterns.Contracts. The default order places the weaver aspect between the default
// and "Build" layers of the contract aspect, but LAMA0042 must not be reported and the weaver must be applied.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.WeaverAndUnusedContractAspect_CrossProject
{
    [RequireAspectWeaver( "Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.WeaverAndUnusedContractAspect_CrossProject.AspectWeaver" )]
    internal class MakeVirtualAttribute : TypeAspect { }

    [MetalamaPlugIn]
    internal class AspectWeaver : IAspectWeaver
    {
        public Task TransformAsync( AspectWeaverContext context )
        {
            return context.RewriteAspectTargetsAsync( new Rewriter() );
        }

        private class Rewriter : SafeSyntaxRewriter
        {
            public override SyntaxNode? VisitBlock( BlockSyntax node )
            {
                return node.WithStatements( node.Statements.Insert( 0, SyntaxFactory.ParseStatement( "global::System.Console.WriteLine(\"Added by weaver.\");" ) ) );
            }
        }
    }

    // <target>
    [MakeVirtual]
    internal class TargetCode
    {
        public string Process( string input )
        {
            return input;
        }
    }
}
