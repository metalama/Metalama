// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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