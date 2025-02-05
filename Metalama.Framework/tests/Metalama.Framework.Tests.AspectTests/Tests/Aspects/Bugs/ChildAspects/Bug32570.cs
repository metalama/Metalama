// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32570;

// <target>
public class TargetType
{
    public int Foo( int x )
    {
        Console.WriteLine( "Original" );

        return x;
    }

    public void Foo_Void( int x )
    {
        Console.WriteLine( "Original" );
    }
}