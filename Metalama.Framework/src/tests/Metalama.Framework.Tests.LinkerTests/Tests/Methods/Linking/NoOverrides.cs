// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Linking.NoOverrides
{
    public class Base
    {
        public void BaseMethod()
        {
        }

        public static void BaseStaticMethod()
        {
        }

        public virtual void BaseVirtualMethod()
        {
        }

        public virtual void BaseVirtualOverriddenMethod()
        {
        }

        public virtual void BaseVirtualHiddenMethod()
        {
        }

        public void BaseHiddenMethod()
        {
        }

        public static void BaseStaticHiddenMethod()
        {
        }
    }

    [PseudoLayerOrder("TestAspect")]
    // <target>
    public class Target : Base
    {

        public override void BaseVirtualOverriddenMethod()
        {
        }

        public new virtual void BaseVirtualHiddenMethod()
        {
        }

        public new void BaseHiddenMethod()
        {
        }

        public static new void BaseStaticHiddenMethod()
        {
        }

        public void LocalMethod()
        {
        }

        public virtual void LocalVirtualMethod()
        {
        }

        public static void LocalStaticMethod()
        {
        }

        public void Foo()
        {
        }

        [PseudoOverride(nameof(Foo), "TestAspect")]
        public void Foo_Override()
        {
            // Should invoke this.
            Link(This.BaseMethod, Api.Base)();
            // Should invoke this.
            Link(This.BaseMethod, Previous )();
            // Should invoke this.
            Link(This.BaseMethod, Current)();
            // Should invoke this.
            Link(This.BaseMethod, Final)();

            // Should invoke current type.
            Link(Static.Target.BaseStaticMethod, Api.Base)();
            // Should invoke current type.
            Link(Static.Target.BaseStaticMethod, Previous )();
            // Should invoke current type.
            Link(Static.Target.BaseStaticMethod, Current)();
            // Should invoke current type.
            Link(Static.Target.BaseStaticMethod, Final)();

            // Should invoke base.
            Link(This.BaseVirtualMethod, Api.Base)();
            // Should invoke base.
            Link(This.BaseVirtualMethod, Previous )();
            // Should invoke base.
            Link(This.BaseVirtualMethod, Current)();
            // Should invoke this.
            Link(This.BaseVirtualMethod, Final)();

            // Should invoke _Source.
            Link(This.BaseVirtualOverriddenMethod, Api.Base)();
            // Should invoke _Source.
            Link(This.BaseVirtualOverriddenMethod, Previous )();
            // Should invoke _Source.
            Link(This.BaseVirtualOverriddenMethod, Current)();
            // Should invoke this.
            Link(This.BaseVirtualOverriddenMethod, Final)();

            // Should invoke _Source.
            Link(This.BaseVirtualHiddenMethod, Api.Base)();
            // Should invoke _Source.
            Link(This.BaseVirtualHiddenMethod, Previous )();
            // Should invoke _Source.
            Link(This.BaseVirtualHiddenMethod, Current)();
            // Should invoke this.
            Link(This.BaseVirtualHiddenMethod, Final)();

            // Should invoke this.
            Link(This.BaseHiddenMethod, Api.Base)();
            // Should invoke this.
            Link(This.BaseHiddenMethod, Previous )();
            // Should invoke this.
            Link(This.BaseHiddenMethod, Current)();
            // Should invoke this.
            Link(This.BaseHiddenMethod, Final)();

            // Should invoke current type.
            Link(Static.Target.BaseStaticHiddenMethod, Api.Base)();
            // Should invoke current type.
            Link(Static.Target.BaseStaticHiddenMethod, Previous )();
            // Should invoke current type.
            Link(Static.Target.BaseStaticHiddenMethod, Current)();
            // Should invoke current type.
            Link(Static.Target.BaseStaticHiddenMethod, Final)();

            // Should invoke this.
            Link(This.LocalMethod, Api.Base)();
            // Should invoke this.
            Link(This.LocalMethod, Previous )();
            // Should invoke this.
            Link(This.LocalMethod, Current)();
            // Should invoke this.
            Link(This.LocalMethod, Final)();

            // Should invoke _Source.
            Link(This.LocalVirtualMethod, Api.Base)();
            // Should invoke _Source.
            Link(This.LocalVirtualMethod, Previous )();
            // Should invoke _Source.
            Link(This.LocalVirtualMethod, Current)();
            // Should invoke this.
            Link(This.LocalVirtualMethod, Final)();

            // Should invoke current type.
            Link(Static.Target.LocalStaticMethod, Api.Base)();
            // Should invoke current type.
            Link(Static.Target.LocalStaticMethod, Previous )();
            // Should invoke current type.
            Link(Static.Target.LocalStaticMethod, Current)();
            // Should invoke current type.
            Link(Static.Target.LocalStaticMethod, Final)();
        }
    }
}
