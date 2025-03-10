// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33446;

// <target>
internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        return GetNumbers();

        IEnumerable<int> GetNumbers()
        {
            yield return 42;
        }
    }
}

// <target>
public class Target
{
    [Aspect]
    public IEnumerable<int> Foo()
    {
        return Array.Empty<int>();
    }
}