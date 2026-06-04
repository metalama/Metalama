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

// This test reproduces issue #1636: a weaver-based aspect (IAspectWeaver) used together with a
// ContractAspect-based aspect in the same compilation, with NO explicit [assembly: AspectOrder]
// tying them together, used to crash with a KeyNotFoundException in PipelineStepIdComparer.Compare
// (reported as LAMA0601).
//
// A ContractAspect has two layers: the default layer and a secondary "Build" layer, ordered
// (default -> Build). A weaver-based aspect has no ordering relationship with the contract, so it
// is sorted only by its (descending) name. The aspect names here are chosen so that the weaver
// aspect sorts BETWEEN the contract's default and "Build" layers (i.e. ordered list is
// [NotNullAttribute, MakeVirtualAttribute, NotNullAttribute:Build]) — exactly as happens with the
// real Metalama.Patterns.Contracts / Metalama.Community.Virtuosity package names. The weaver then
// splits the high-level pipeline into two stages around itself, leaving the contract's "Build"
// layer in a different stage than its default layer, which used to throw the KeyNotFoundException.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.WeaverAndContractAspect
{
    [RequireAspectWeaver( "Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.WeaverAndContractAspect.AspectWeaver" )]
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
