// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER && NET8_0_OR_GREATER

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.CompoundAssignmentOperator_Override_Generic;

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Overridden." );

        return meta.Proceed();
    }
}

// <target>
public class C<T>
{
    private int _x;

    public C( int x ) {
        this._x = x;
    }

    [TheAspect]
    public void operator += ( C<T> value )
    {
        this._x += value._x;
    }

    [TheAspect]
    public void operator ++()
    {
        this._x++;
    }

    public void M()
    {
        var c = new C<T>(1);
        Console.WriteLine( c += new C<T>(2) );
    }
}

#endif