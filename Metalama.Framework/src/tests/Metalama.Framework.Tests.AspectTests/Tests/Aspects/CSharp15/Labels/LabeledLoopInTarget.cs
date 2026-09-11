// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
#endif

// The target method carries a labeled loop, a labeled break and a labeled continue, and two aspects override it. The
// template annotator runs inside a template only, so it does not report the run-time code of the target, and the
// inlining has to keep the jumps targeting the loop they targeted in the source. See issue #1947.
//
// The project compiles its own test sources at the language version of LangMaxVersion, which is 14.0 and does not
// accept a labeled break. The project file therefore removes this file from the compilation. The test framework
// reads the file from the source directory, so the test still runs.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.Labels.LabeledLoopInTarget;

internal class OuterAspectAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Outer aspect." );

        return meta.Proceed();
    }
}

internal class InnerAspectAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Inner aspect." );

        return meta.Proceed();
    }
}

// <target>
internal class TargetType
{
    [OuterAspect]
    [InnerAspect]
    public int Method( int limit )
    {
        var total = 0;

    outer:

        for ( var i = 0; i < 3; i++ )
        {
            for ( var j = 0; j < 3; j++ )
            {
                if ( j == 1 )
                {
                    continue outer;
                }

                total += i + j;

                if ( total > limit )
                {
                    break outer;
                }
            }
        }

        return total;
    }
}
