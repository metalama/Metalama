// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Include(_Tagging.cs)
#endif

using Metalama.Extensions.Multicast;
using Metalama.Extensions.Multicast.AspectTests;

[assembly: AddTag(
    "Prefixed",
    AttributeTargetElements = MulticastTargets.Class,
    AttributeTargetTypes = "Metalama.Extensions.Multicast.AspectTests.Filter_ByTypeName_Wildcards.Prefixed*" )]

[assembly: AddTag(
    "Ns",
    AttributeTargetElements = MulticastTargets.Class,
    AttributeTargetTypes = "Metalama.Extensions.Multicast.AspectTests.Filter_ByTypeName_Wildcards.Ns.*" )]

namespace Metalama.Extensions.Multicast.AspectTests.Filter_ByTypeName_Wildcards
{
    // <target>
    public class PrefixedA { }

    // <target>
    public class PrefixedB { }

    // <target>
    public class NonPrefixed { }

    // <target>
    namespace Ns
    {
        public class InNamespace { }
    }
}