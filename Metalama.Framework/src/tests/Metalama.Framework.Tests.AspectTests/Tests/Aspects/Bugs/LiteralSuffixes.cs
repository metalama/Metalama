// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.LiteralSuffixes;

public class TestAspect : OverrideMethodAspect
{
    public override dynamic OverrideMethod()
    {
        var f1 = 0.5f;
        var f2 = f1;

        var d1 = 0.5d;
        var d2 = d1;

        var m1 = 0.5m;
        var m2 = m1;

        var u1 = 0u;
        var u2 = u1;

        var l1 = 0L;
        var l2 = l1;

        var ul1 = 0UL;
        var ul2 = ul1;

        var x1 = 0x12345678ABCDEFul;
        var x2 = x1;

        var b1 = 0b01_10;
        var b2 = b1;

        return meta.Proceed();
    }
}

public class TargetClass
{
    // <target>
    [TestAspect]
    public void Method() { }
}