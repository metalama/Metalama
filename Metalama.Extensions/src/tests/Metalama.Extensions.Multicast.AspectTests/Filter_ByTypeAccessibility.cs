// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Include(_Tagging.cs)
#endif

using Metalama.Extensions.Multicast;
using Metalama.Extensions.Multicast.AspectTests;

[assembly: AddTag(
    "PublicType",
    AttributeTargetElements = MulticastTargets.Class,
    AttributeTargetTypeAttributes = MulticastAttributes.Public )]

[assembly: AddTag(
    "MethodOfPublicType",
    AttributeTargetElements = MulticastTargets.Method,
    AttributeTargetTypeAttributes = MulticastAttributes.Public )]

namespace Metalama.Extensions.Multicast.AspectTests.Filter_ByTypeAccessibility
{
    // <target>
    public class PublicClass
    {
        public void PublicMethod() { }

        internal void InternalMethod() { }
    }

    // <target>
    internal class InternalClass
    {
        public void PublicMethod() { }

        internal void InternalMethod() { }
    }
}