// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable CS8602

// This test covers issues #1636 and #2226. It is the same as WeaverAndContractAspect, but the contract aspect
// has two instances. The default order places the weaver aspect between the default and "Build" layers of the
// contract aspect, so LAMA0042 must be reported. It must be reported only once for the aspect class, and not once
// for each instance.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.WeaverAndContractAspect_SeveralInstances
{
    [RequireAspectWeaver( "Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.WeaverAndContractAspect_SeveralInstances.AspectWeaver" )]
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

    internal class NotNullAttribute : ContractAspect
    {
        public override void Validate( dynamic? value )
        {
            if ( value == null )
            {
                throw new ArgumentNullException( meta.Target.Parameter.Name );
            }
        }
    }

    // <target>
    [MakeVirtual]
    internal class TargetCode
    {
        public string Process( [NotNull] string input, [NotNull] string other )
        {
            return input;
        }
    }
}
