// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.CompoundAssignmentOperator_Override;

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Overridden." );

        return meta.Proceed();
    }
}

// <target>
public class C
{
    private int _x;

    [TheAspect]
    public void operator += ( int value )
    {
        this._x += value;
    }

    [TheAspect]
    public void operator ++()
    {
        this._x++;
    }

    public void M()
    {
        var c = new C();
        Console.WriteLine( c += 5 );
    }
}

#endif