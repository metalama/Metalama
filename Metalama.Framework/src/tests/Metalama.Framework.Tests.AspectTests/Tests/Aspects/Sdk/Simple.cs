// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.Simple
{
    [RequireAspectWeaver( "Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.Simple.AspectWeaver" )]
    internal class Aspect : MethodAspect { }

    [MetalamaPlugIn]
    internal class AspectWeaver : IAspectWeaver
    {
        public Task TransformAsync( AspectWeaverContext context )
        {
            return context.RewriteAspectTargetsAsync( new Rewriter() );
        }

        private class Rewriter : SafeSyntaxRewriter
        {
            public override SyntaxNode VisitMethodDeclaration( MethodDeclarationSyntax node )
            {
                return base.VisitMethodDeclaration( node )!.WithLeadingTrivia( SyntaxFactory.Comment( "// Rewritten." ), SyntaxFactory.CarriageReturnLineFeed );
            }
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        private int TransformedMethod( int a ) => 0;

        private int NotTransformedMethod( int a ) => 0;
    }
}