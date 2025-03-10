// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32744;

#pragma warning disable CS8321
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

public class TestAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        return meta.Proceed();

        void Foo( int i ) { }
    }
}

internal class C
{
    // <target>
    [Test]
    private static int Bar()
    {
        return 42;
    }
}