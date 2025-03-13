// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;

namespace Aspect
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var symbol = builder.Target.GetSymbol();

            var id = symbol.GetDocumentationCommentId();

            builder.Advice.IntroduceMethod( builder.Target, nameof(Bar), args: new { s = id } );
        }

        [Template]
        public static void Bar( [CompileTime] string s )
        {
            Console.WriteLine( s );
        }
    }
}