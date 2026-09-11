// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;
using System;

namespace Metalama.Framework.Tests.AspectTests.Templating.UnsupportedSyntax.GeneratedLabelIsKept
{
    [CompileTime]
    internal class Aspect
    {
        // The template compiler rewriter generates a labeled statement and a goto statement of its own for a run-time
        // statement that follows a return inside a compile-time condition. That generation does not pass through the
        // template annotator, so the rejection of a label in a template does not affect it. The expected compiled
        // template beside this file is what pins it. See issue #1947.
        [TestTemplate]
        private dynamic? Template()
        {
            if ( meta.Target.Method.Name.Length > 0 )
            {
                return meta.Proceed();
            }

            Console.WriteLine( "After the compile-time condition." );

            return null;
        }
    }

    internal class TargetCode
    {
        // <target>
        private object? Method()
        {
            return "test";
        }
    }
}
