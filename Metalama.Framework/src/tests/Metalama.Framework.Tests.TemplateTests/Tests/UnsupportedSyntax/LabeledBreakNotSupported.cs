// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @RequiredConstant(ROSLYN_5_11_0_OR_GREATER)
#endif

// A labeled break in a template is not supported, for the reason given in section 5 of
// Metalama.Framework/docs/2027.0/DECISIONS.md: the label belongs to a loop whose scope may differ from the scope of
// the statement that names it, so the annotator cannot classify the statement. See issue #1947.
//
// The project compiles its own test sources at the language version of LangMaxVersion, which is 14.0 and does not
// accept a labeled break. The project file therefore removes this file from the compilation. The test framework
// reads the file from the source directory, so the test still runs.
//
// The template also declares the label that the break statement names, so the expected output reports the label too.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;
using System;

namespace Metalama.Framework.Tests.AspectTests.Templating.UnsupportedSyntax.LabeledBreakNotSupported
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
        outer:

            for ( var i = 0; i < 2; i++ )
            {
                for ( var j = 0; j < 2; j++ )
                {
                    Console.WriteLine( i + j );

                    break outer;
                }
            }

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private int Method( int a, int b )
        {
            return a + b;
        }
    }
}
