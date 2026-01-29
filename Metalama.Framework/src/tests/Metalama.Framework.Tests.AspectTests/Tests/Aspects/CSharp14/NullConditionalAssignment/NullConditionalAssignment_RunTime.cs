// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.NullConditionalAssignment_RunTime;

internal class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        // Pure run-time null-conditional assignment
        var target = (Target?)meta.Target.Parameters[0].Value;
        target?.Property = 42;

        Console.WriteLine( "Intercepted" );

        return meta.Proceed();
    }
}

internal class Target
{
    public int Property { get; set; }
}

// <target>
internal class C
{
    [TheAspect]
    public void M( Target? t )
    {
    }
}

#endif
