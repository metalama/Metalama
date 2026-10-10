// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.DiagnosticReview.LAMA0105_SplitVariablesCrash;

// Regression test for https://github.com/metalama/Metalama/issues/2154.
// A local declaration that mixes a compile-time and a run-time variable must report LAMA0105 without crashing the template compiler.

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        int a = meta.CompileTime( 0 ), b = 0;

        return meta.Proceed();
    }
}

// <target>
internal class C
{
    [TheAspect]
    private void M() { }
}
