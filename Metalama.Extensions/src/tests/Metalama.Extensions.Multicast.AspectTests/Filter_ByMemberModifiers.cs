// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Include(_Tagging.cs)
#endif

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// <target>
namespace Metalama.Extensions.Multicast.AspectTests.Filter_ByMemberModifiers
{
    [AddTag( "Abstract", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.Abstract )]
    [AddTag( "NonAbstract", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.NonAbstract )]
    public abstract class ByAbstraction
    {
        public abstract void AbstractMethod();

        public void NonAbstract() { }
    }

    [AddTag( "Virtual", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.Virtual )]
    [AddTag( "NonVirtual", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.NonVirtual )]
    public class ByVirtuality
    {
        public virtual void VirtualMethod() { }

        public void NonVirtualMethod() { }
    }

    [AddTag( "Static", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.Static )]
    [AddTag( "Instance", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.Instance )]
    public class ByScope
    {
        public void InstanceMethod() { }

        public static void StaticMethod() { }
    }

    [AddTag( "Managed", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.Managed )]
    [AddTag( "NonManaged", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.NonManaged )]
    public class ByImplementation
    {
        [DllImport( "foo" )]
        public static extern void ExternMethod();

        public static void ManagedMethod() { }
    }

    [AddTag(
        "CompilerGenerated",
        AttributeTargetElements = MulticastTargets.AnyMember,
        AttributeTargetMemberAttributes = MulticastAttributes.CompilerGenerated )]
    [AddTag( "UserGenerated", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.UserGenerated )]
    public class ByGeneration
    {
        [CompilerGenerated]
        public static void CompilerGeneratedMethod() { }

        public void UserGenerated() { }
    }

    [AddTag( "Literal", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.Literal )]
    [AddTag( "NonLiteral", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.NonLiteral )]
    public class ByLiteral
    {
        public const int ConstField = 5;
        public int NonConstField;
    }
}