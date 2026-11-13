// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.InheritableAspectBeforeWeaver;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof( InheritableAspect ), typeof( WeaverAspect ) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.InheritableAspectBeforeWeaver
{
    [Inheritable]
    public class InheritableAspect : TypeAspect
    {
        [Introduce]
        public void Introduced() { }

        public override void BuildEligibility( IEligibilityBuilder<INamedType> builder )
        {
            base.BuildEligibility( builder );
            builder.ExceptForInheritance().MustNotBeInterface();
        }
    }

    [RequireAspectWeaver( "Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.InheritableAspectBeforeWeaver.AspectWeaver" )]
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

    [InheritableAspect]
    public interface I { }

    internal class WeavedCode
    {
        [WeaverAspect]
        private void M() { }
    }
}
