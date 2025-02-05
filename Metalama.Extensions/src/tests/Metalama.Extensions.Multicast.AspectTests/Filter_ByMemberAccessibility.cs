// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @Include(_Tagging.cs)
#endif

using Metalama.Extensions.Multicast;
using Metalama.Extensions.Multicast.AspectTests;

// ReSharper disable EventNeverSubscribedTo.Global

[assembly: AddTag(
    "PublicMember",
    AttributeTargetElements = MulticastTargets.Method | MulticastTargets.Property | MulticastTargets.Event | MulticastTargets.Field,
    AttributeTargetTypes = "Metalama.Extensions.Multicast.AspectTests.Filter_ByMemberAccessibility.*",
    AttributeTargetMemberAttributes = MulticastAttributes.Public )]

// <target>
namespace Metalama.Extensions.Multicast.AspectTests.Filter_ByMemberAccessibility
{
    public class PublicClass
    {
        public void PublicMethod() { }

        internal void InternalMethod() { }

        public int PublicProperty { get; private set; }

        internal int InternalProperty { get; private set; }

        public int PublicField;
        internal int InternalField;

        public event Action PublicEvent;

        internal event Action InternalEvent;
    }

    internal class InternalClass
    {
        public void PublicMethod() { }

        internal void InternalMethod() { }

        public int PublicProperty { get; private set; }

        internal int InternalProperty { get; private set; }

        public int PublicField;
        internal int InternalField;

        public event Action PublicEvent;

        internal event Action InternalEvent;
    }
}