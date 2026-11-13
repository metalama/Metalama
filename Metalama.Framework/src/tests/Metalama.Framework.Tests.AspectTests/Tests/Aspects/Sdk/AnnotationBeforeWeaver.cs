// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using System.Threading.Tasks;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.AnnotationBeforeWeaver;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// The weaver modifies the compilation, so the compilation model of the stage after the weaver is created from the
// annotations of the result of the weaver stage.
[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof( AddAnnotationAspect ), typeof( WeaverAspect ), typeof( ReadAnnotationAspect ) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.AnnotationBeforeWeaver
{
    public class MyAnnotation : IAnnotation<INamedType>
    {
        public MyAnnotation( string? value )
        {
            Value = value;
        }

        public string? Value { get; }
    }

    public class AddAnnotationAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.AddAnnotation( new MyAnnotation( "TheValue" ) );
        }
    }

    public class ReadAnnotationAspect : TypeAspect
    {
        [Introduce]
        public string? TheAnnotation = meta.Target.Type.Enhancements().GetAnnotations<MyAnnotation>().SingleOrDefault()?.Value;
    }

    [RequireAspectWeaver( "Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.AnnotationBeforeWeaver.AspectWeaver" )]
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

    internal class WeavedCode
    {
        [WeaverAspect]
        private void M() { }
    }

    // <target>
    [AddAnnotationAspect]
    [ReadAnnotationAspect]
    internal class C { }
}
