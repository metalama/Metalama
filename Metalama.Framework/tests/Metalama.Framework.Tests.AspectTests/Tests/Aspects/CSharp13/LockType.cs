// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_12_0_OR_GREATER)
// @RequiredConstant(NET9_0_OR_GREATER)
#endif

#if ROSLYN_4_12_0_OR_GREATER && NET9_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Threading;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.LockType;

public class TheAspect : OverrideMethodAspect
{
    [Introduce]
    Lock _aspectLock = new();

    public override dynamic? OverrideMethod()
    {
        lock (_aspectLock)
        {
            return meta.Proceed();
        }
    }
}

// <target>
class Target
{
    Lock _lock = new();

    [TheAspect]
    void M()
    {
        lock (_lock)
        {
            Console.WriteLine("Hello, World!");
        }
    }
}

#endif