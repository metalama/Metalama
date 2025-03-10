// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.WeaverAndRegularAspects_AddAspect;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(RegularAspect1), typeof(WeaverAspect), typeof(RegularAspect2), typeof(CombinedAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.WeaverAndRegularAspects_AddAspect
{
    [RequireAspectWeaver( "Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.WeaverAndRegularAspects_AddAspect.AspectWeaver" )]
    internal class WeaverAspect : MethodAspect { }

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
                return node.WithStatements( node.Statements.Insert( 0, SyntaxFactory.ParseStatement( "Console.WriteLine(\"Added by weaver.\");" ) ) );
            }
        }
    }

    internal class RegularAspect1 : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "Added by regular aspect #1." );

            return meta.Proceed();
        }
    }

    internal class RegularAspect2 : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "Added by regular aspect #2." );

            return meta.Proceed();
        }
    }

    internal class CombinedAspect : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            builder.AddAspect<RegularAspect1>();
            builder.AddAspect<WeaverAspect>();
            builder.AddAspect<RegularAspect2>();
        }
    }

    // <target>
    internal class TargetCode
    {
        [CombinedAspect]
        private void M() { }
    }
}