using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.NestedApplication
{
    [RequireAspectWeaver( "Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.NestedApplication.AspectWeaver" )]
    internal class Aspect : TypeAspect { }

    [MetalamaPlugIn]
    internal class AspectWeaver : IAspectWeaver
    {
        public Task TransformAsync( AspectWeaverContext context )
        {
            return context.RewriteAspectTargetsAsync( new Rewriter() );
        }

        private class Rewriter : SafeSyntaxRewriter
        {
            public override SyntaxNode? VisitClassDeclaration( ClassDeclarationSyntax node )
            {
                var rewrittenNode = base.VisitClassDeclaration( node )!;

                return rewrittenNode.WithLeadingTrivia(
                    rewrittenNode.GetLeadingTrivia().AddRange( new[] { SyntaxFactory.Comment( "// Rewritten." ), SyntaxFactory.CarriageReturnLineFeed } ) );
            }
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        internal class Outer
        {
            [Aspect]
            internal class Inner { }
        }
    }
}