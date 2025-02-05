// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @Include(_Tagging.cs)
#endif

namespace Metalama.Extensions.Multicast.AspectTests.MulticastOnInheritance
{
    // <target>
    [AddTagInherited( "Tagged", true )]
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