// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.DynamicInLambda;

#pragma warning disable CS0618 // Type or member is obsolete

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( string.Join( ", ", meta.Target.Parameters.Select( p => p.Value ) ) );

        return meta.Proceed();
    }
}

internal class TargetCode
{
    [Aspect]
    private int Method( int a, int b )
    {
        return a + b;
    }
}
