// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @Include(_Tagging.cs)
#endif

using Metalama.Extensions.Multicast;
using Metalama.Extensions.Multicast.AspectTests;

[assembly: AddTag(
    "C",
    AttributeTargetElements = MulticastTargets.Class,
    AttributeTargetTypes = "Metalama.Extensions.Multicast.AspectTests.Filter_ByTypeName.C" )]

[assembly: AddTag(
    "C+N",
    AttributeTargetElements = MulticastTargets.Class,
    AttributeTargetTypes = "Metalama.Extensions.Multicast.AspectTests.Filter_ByTypeName.C+N" )]

[assembly: AddTag(
    "C<T>",
    AttributeTargetElements = MulticastTargets.Class,
    AttributeTargetTypes = "Metalama.Extensions.Multicast.AspectTests.Filter_ByTypeName.C`1" )]

[assembly: AddTag(
    "C<T>+N",
    AttributeTargetElements = MulticastTargets.Class,
    AttributeTargetTypes = "Metalama.Extensions.Multicast.AspectTests.Filter_ByTypeName.C`1+N" )]

namespace Metalama.Extensions.Multicast.AspectTests.Filter_ByTypeName
{
    // <target>
    public class C
    {
        private class N { }
    }

    // <target>
    public class D { }

    // <target>
    public class C<T>
    {
        private class N { }
    }
}