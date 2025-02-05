// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Multicast.AspectTests.HelloWorld;
using Metalama.Framework.Aspects;

#if TEST_OPTIONS
// @Include(_Tagging.cs)
#endif

[assembly: MyAspect( "Hello, world." )]

namespace Metalama.Extensions.Multicast.AspectTests.HelloWorld
{
    public class MyAspect : OverrideMethodMulticastAspect
    {
        private readonly string _tag;

        public MyAspect( string tag )
        {
            this._tag = tag;
        }

        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( $"Overridden: {this._tag}" );

            return meta.Proceed();
        }
    }

    // <target>
    public class C
    {
        public void M() { }
    }
}