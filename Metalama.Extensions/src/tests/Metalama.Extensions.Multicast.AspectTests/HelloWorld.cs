// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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