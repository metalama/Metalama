// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.WeaverAndContractAspectOrdered;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable CS8602

// This is the working, explicitly-ordered counterpart of WeaverAndContractAspect (issue #1636).
// The aspect names are still chosen so that, without ordering, the weaver would sort between the
// contract's default and "Build" layers. The [assembly: AspectOrder] below orders the weaver before
// all layers of the contract, so the contract's two layers stay in a single high-level stage and no
// LAMA0042 is reported. Both the weaver transformation and the contract are applied.

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof( MakeVirtualAttribute ), typeof( NotNullAttribute ) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.WeaverAndContractAspectOrdered
{
    [RequireAspectWeaver( "Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.WeaverAndContractAspectOrdered.AspectWeaver" )]
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
        public string Process( [NotNull] string input )
        {
            return input;
        }
    }
}
