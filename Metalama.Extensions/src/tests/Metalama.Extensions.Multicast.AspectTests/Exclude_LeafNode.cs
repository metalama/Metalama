// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Extensions.Multicast.AspectTests.Exclude_LeafNode;
using Metalama.Framework.Aspects;

#if TEST_OPTIONS
// @Include(_Tagging.cs)
#endif

[assembly: MyAspect( "1" )]

namespace Metalama.Extensions.Multicast.AspectTests.Exclude_LeafNode
{
    [AttributeUsage( AttributeTargets.Assembly | AttributeTargets.Method, AllowMultiple = true )]
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
        [MyAspect( "2", AttributeExclude = true )]
        public void M() { }
    }
}