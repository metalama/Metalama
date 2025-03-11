// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.IO;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33599;

public class Test1Attribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.Proceed();

        return default;
    }
}

public class Test2Attribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        _ = meta.Proceed();

        return default;
    }
}

// <target>
internal class Target
{
    [Test1]
    public MemoryStream M1()
    {
        return new MemoryStream();
    }

    [Test2]
    public MemoryStream M2()
    {
        return new MemoryStream();
    }

    [Test1]
    public MemoryStream M3() => new();

    [Test2]
    public MemoryStream M4() => new();
}