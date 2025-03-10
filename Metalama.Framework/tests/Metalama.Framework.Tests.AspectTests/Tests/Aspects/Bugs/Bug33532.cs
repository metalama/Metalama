// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33532;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        return meta.Proceed();

        throw new NotImplementedException();
    }
}

// <target>
internal class Target
{
    [Aspect]
    private static void UnreachableAfterReturn()
    {
        return;

        throw new Exception();
    }

    [Aspect]
    private static void ReachableAfterReturn( int i )
    {
        if (i == 0)
        {
            goto label;
        }

        return;

    label:
        Console.WriteLine( "Test" );
    }
}