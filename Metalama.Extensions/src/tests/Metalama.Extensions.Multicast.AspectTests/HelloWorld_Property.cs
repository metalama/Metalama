// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Multicast.AspectTests.HelloWorld_Property;
using Metalama.Framework.Aspects;

#if TEST_OPTIONS
// @Include(_Tagging.cs)
#endif

[assembly: MyAspect( "Hello, world." )]

namespace Metalama.Extensions.Multicast.AspectTests.HelloWorld_Property;

public class MyAspect : OverrideFieldOrPropertyMulticastAspect
{
    private readonly string _tag;

    public MyAspect( string tag )
    {
        this._tag = tag;
    }

    public override dynamic? OverrideProperty
    {
        get
        {
            Console.WriteLine( $"Overridden get: {this._tag}" );

            return meta.Proceed();
        }
        set
        {
            Console.WriteLine( $"Overridden set: {this._tag}" );

            meta.Proceed();
        }
    }
}

// <target>
public class C
{
    private readonly string _f;

    public int P { get; set; }
}