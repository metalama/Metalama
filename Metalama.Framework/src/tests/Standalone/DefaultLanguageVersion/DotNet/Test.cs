// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace DefaultLanguageVersion;

public class TestAspect : OverrideMethodAspect
{
    public override dynamic OverrideMethod()
    {
        Console.WriteLine( "overridden" );

        return meta.Proceed();
    }
}

internal class Target
{
    // <target>
    [TestAspect]
    public static void M()
    {
        int[] a = [];
    }
}