// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Include(_Tagging.cs)
#endif

namespace Metalama.Extensions.Multicast.AspectTests.Inheritance
{
    // <target>
    [AddTagInherited( "Tagged" )]
    public class C
    {
        protected virtual void M() { }
    }

    // <target>
    public class D : C
    {
        protected override void M() { }

        protected void N() { }
    }
}