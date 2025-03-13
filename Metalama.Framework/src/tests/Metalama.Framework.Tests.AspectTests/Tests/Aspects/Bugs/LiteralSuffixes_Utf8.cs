// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.LiteralSuffixes_Utf8;

public class TestAspect : OverrideMethodAspect
{
    public override dynamic OverrideMethod()
    {
        var s1 = "littoral literal"u8;
        var s2 = s1;

        return meta.Proceed();
    }
}

public class TargetClass
{
    // <target>
    [TestAspect]
    public void Method() { }
}