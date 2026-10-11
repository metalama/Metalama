// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

#if TEST_OPTIONS
// @EnableLogging
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.TestFramework.EnableLogging;

// Regression test for https://github.com/metalama/Metalama/issues/2224: the @EnableLogging option must not fail.

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        return meta.Proceed();
    }
}

internal class C
{
    [TheAspect]
    public void M() { }
}
