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

        public new static void BaseStaticHiddenMethod()
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
            link(_this.BaseMethod, @base)();
            // Should invoke this.
            link(_this.BaseMethod, previous)();
            // Should invoke this.
            link(_this.BaseMethod, current)();
            // Should invoke this.
            link(_this.BaseMethod, final)();

            // Should invoke current type.
            link(_static.Target.BaseStaticMethod, @base)();
            // Should invoke current type.
            link(_static.Target.BaseStaticMethod, previous)();
            // Should invoke current type.
            link(_static.Target.BaseStaticMethod, current)();
            // Should invoke current type.
            link(_static.Target.BaseStaticMethod, final)();

            // Should invoke base.
            link(_this.BaseVirtualMethod, @base)();
            // Should invoke base.
            link(_this.BaseVirtualMethod, previous)();
            // Should invoke base.
            link(_this.BaseVirtualMethod, current)();
            // Should invoke this.
            link(_this.BaseVirtualMethod, final)();

            // Should invoke _Source.
            link(_this.BaseVirtualOverriddenMethod, @base)();
            // Should invoke _Source.
            link(_this.BaseVirtualOverriddenMethod, previous)();
            // Should invoke _Source.
            link(_this.BaseVirtualOverriddenMethod, current)();
            // Should invoke this.
            link(_this.BaseVirtualOverriddenMethod, final)();

            // Should invoke _Source.
            link(_this.BaseVirtualHiddenMethod, @base)();
            // Should invoke _Source.
            link(_this.BaseVirtualHiddenMethod, previous)();
            // Should invoke _Source.
            link(_this.BaseVirtualHiddenMethod, current)();
            // Should invoke this.
            link(_this.BaseVirtualHiddenMethod, final)();

            // Should invoke this.
            link(_this.BaseHiddenMethod, @base)();
            // Should invoke this.
            link(_this.BaseHiddenMethod, previous)();
            // Should invoke this.
            link(_this.BaseHiddenMethod, current)();
            // Should invoke this.
            link(_this.BaseHiddenMethod, final)();

            // Should invoke current type.
            link(_static.Target.BaseStaticHiddenMethod, @base)();
            // Should invoke current type.
            link(_static.Target.BaseStaticHiddenMethod, previous)();
            // Should invoke current type.
            link(_static.Target.BaseStaticHiddenMethod, current)();
            // Should invoke current type.
            link(_static.Target.BaseStaticHiddenMethod, final)();

            // Should invoke this.
            link(_this.LocalMethod, @base)();
            // Should invoke this.
            link(_this.LocalMethod, previous)();
            // Should invoke this.
            link(_this.LocalMethod, current)();
            // Should invoke this.
            link(_this.LocalMethod, final)();

            // Should invoke _Source.
            link(_this.LocalVirtualMethod, @base)();
            // Should invoke _Source.
            link(_this.LocalVirtualMethod, previous)();
            // Should invoke _Source.
            link(_this.LocalVirtualMethod, current)();
            // Should invoke this.
            link(_this.LocalVirtualMethod, final)();

            // Should invoke current type.
            link(_static.Target.LocalStaticMethod, @base)();
            // Should invoke current type.
            link(_static.Target.LocalStaticMethod, previous)();
            // Should invoke current type.
            link(_static.Target.LocalStaticMethod, current)();
            // Should invoke current type.
            link(_static.Target.LocalStaticMethod, final)();
        }
    }
}
