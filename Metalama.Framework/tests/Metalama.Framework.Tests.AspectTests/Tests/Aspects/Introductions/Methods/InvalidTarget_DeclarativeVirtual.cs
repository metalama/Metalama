// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.InvalidTarget_DeclarativeVirtual
{
    /*
     * Tests cases where method is introduced as virtual into a type where virtual members are not allowed.
     */

    public class ImplicitlyInstanceExplicitlyVirtualIntroductionAttribute : TypeAspect
    {
        [Introduce( IsVirtual = true )]
        public int Method_ImplicitlyInstanceExplicitlyVirtual()
        {
            return meta.Proceed();
        }
    }

    public class ImplicitlyInstanceImplicitlyVirtualIntroductionAttribute : TypeAspect
    {
        [Introduce]
        public virtual int Method_ImplicitlyInstanceImplicitlyVirtual()
        {
            return meta.Proceed();
        }
    }

    public class ExplicitlyInstanceExplicitlyVirtualIntroductionAttribute : TypeAspect
    {
        [Introduce( Scope = IntroductionScope.Instance, IsVirtual = true )]
        public static int Method_ExplicitlyInstanceExplicitlyVirtual()
        {
            return meta.Proceed();
        }
    }

    // <target>
    [ImplicitlyInstanceExplicitlyVirtualIntroduction]
    [ImplicitlyInstanceImplicitlyVirtualIntroduction]
    [ExplicitlyInstanceExplicitlyVirtualIntroduction]
    internal struct TargetStruct { }

    // <target>
    [ImplicitlyInstanceExplicitlyVirtualIntroduction]
    [ImplicitlyInstanceImplicitlyVirtualIntroduction]
    [ExplicitlyInstanceExplicitlyVirtualIntroduction]
    internal sealed class SealedTargetClass { }

    // <target>
    [ImplicitlyInstanceExplicitlyVirtualIntroduction]
    [ImplicitlyInstanceImplicitlyVirtualIntroduction]
    [ExplicitlyInstanceExplicitlyVirtualIntroduction]
    internal static class StaticTargetClass { }
}