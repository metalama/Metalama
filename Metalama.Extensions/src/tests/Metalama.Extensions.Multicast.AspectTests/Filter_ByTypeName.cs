// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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