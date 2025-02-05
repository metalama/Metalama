using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Events.Linking.NoOverrides
{
    public class Base
    {
        public event EventHandler BaseMethod
        {
            add
            {
            }
            remove
            {

            }
        }

        public static event EventHandler BaseStaticMethod
        {
            add
            {
            }
            remove
            {
            }
        }

        public virtual event EventHandler BaseVirtualMethod
        {
            add
            {
            }
            remove
            {
            }
        }

        public virtual event EventHandler BaseVirtualOverriddenMethod
        {
            add
            {
            }
            remove
            {
            }
        }

        public virtual event EventHandler BaseVirtualHiddenMethod
        {
            add
            {
            }
            remove
            {
            }
        }

        public event EventHandler BaseHiddenMethod
        {
            add
            {
            }
            remove
            {

            }
        }

        public static event EventHandler BaseStaticHiddenMethod
        {
            add
            {
            }
            remove
            {
            }
        }
    }

    [PseudoLayerOrder("TestAspect")]
    // <target>
    public class Target : Base
    {

        public override event EventHandler BaseVirtualOverriddenMethod
        {
            add
            {
            }
            remove
            {
            }
        }

        public new virtual event EventHandler BaseVirtualHiddenMethod
        {
            add
            {
            }
            remove
            {
            }
        }

        public new event EventHandler BaseHiddenMethod
        {
            add
            {
            }
            remove
            {
            }
        }

        public new static event EventHandler BaseStaticHiddenMethod
        {
            add
            {
            }
            remove
            {
            }
        }

        public event EventHandler LocalMethod
        {
            add
            {
            }
            remove
            {

            }
        }

        public virtual event EventHandler LocalVirtualMethod
        {
            add
            {
            }
            remove
            {
            }
        }

        public static event EventHandler LocalStaticMethod
        {
            add
            {
            }
            remove
            {
            }
        }

        public event System.EventHandler Foo
        {
            add
            {
            }
            remove
            {

            }
        }

        [PseudoOverride(nameof(Foo), "TestAspect")]
        public event System.EventHandler Foo_Override
        {
            add
            {
                // Should invoke this.
                link[_this.BaseMethod.add, @base] += value;
                // Should invoke this.
                link[_this.BaseMethod.add, previous] += value;
                // Should invoke this.
                link[_this.BaseMethod.add, current] += value;
                // Should invoke this.
                link[_this.BaseMethod.add, final] += value;

                // Should invoke current type.
                link[_static.Target.BaseStaticMethod.add, @base] += value;
                // Should invoke current type.
                link[_static.Target.BaseStaticMethod.add, previous] += value;
                // Should invoke current type.
                link[_static.Target.BaseStaticMethod.add, current] += value;
                // Should invoke current type.
                link[_static.Target.BaseStaticMethod.add, final] += value;

                // Should invoke base.
                link[_this.BaseVirtualMethod.add, @base] += value;
                // Should invoke base.
                link[_this.BaseVirtualMethod.add, previous] += value;
                // Should invoke base.
                link[_this.BaseVirtualMethod.add, current] += value;
                // Should invoke this.
                link[_this.BaseVirtualMethod.add, final] += value;

                // Should invoke _Source.
                link[_this.BaseVirtualOverriddenMethod.add, @base] += value;
                // Should invoke _Source.
                link[_this.BaseVirtualOverriddenMethod.add, previous] += value;
                // Should invoke _Source.
                link[_this.BaseVirtualOverriddenMethod.add, current] += value;
                // Should invoke this.
                link[_this.BaseVirtualOverriddenMethod.add, final] += value;

                // Should invoke _Source.
                link[_this.BaseVirtualHiddenMethod.add, @base] += value;
                // Should invoke _Source.
                link[_this.BaseVirtualHiddenMethod.add, previous] += value;
                // Should invoke _Source.
                link[_this.BaseVirtualHiddenMethod.add, current] += value;
                // Should invoke this.
                link[_this.BaseVirtualHiddenMethod.add, final] += value;

                // Should invoke this.
                link[_this.BaseHiddenMethod.add, @base] += value;
                // Should invoke this.
                link[_this.BaseHiddenMethod.add, previous] += value;
                // Should invoke this.
                link[_this.BaseHiddenMethod.add, current] += value;
                // Should invoke this.
                link[_this.BaseHiddenMethod.add, final] += value;

                // Should invoke current type.
                link[_static.Target.BaseStaticHiddenMethod.add, @base] += value;
                // Should invoke current type.
                link[_static.Target.BaseStaticHiddenMethod.add, previous] += value;
                // Should invoke current type.
                link[_static.Target.BaseStaticHiddenMethod.add, current] += value;
                // Should invoke current type.
                link[_static.Target.BaseStaticHiddenMethod.add, final] += value;

                // Should invoke this.
                link[_this.LocalMethod.add, @base] += value;
                // Should invoke this.
                link[_this.LocalMethod.add, previous] += value;
                // Should invoke this.
                link[_this.LocalMethod.add, current] += value;
                // Should invoke this.
                link[_this.LocalMethod.add, final] += value;

                // Should invoke _Source.
                link[_this.LocalVirtualMethod.add, @base] += value;
                // Should invoke _Source.
                link[_this.LocalVirtualMethod.add, previous] += value;
                // Should invoke _Source.
                link[_this.LocalVirtualMethod.add, current] += value;
                // Should invoke this.
                link[_this.LocalVirtualMethod.add, final] += value;

                // Should invoke current type.
                link[_static.Target.LocalStaticMethod.add, @base] += value;
                // Should invoke current type.
                link[_static.Target.LocalStaticMethod.add, previous] += value;
                // Should invoke current type.
                link[_static.Target.LocalStaticMethod.add, current] += value;
                // Should invoke current type.
                link[_static.Target.LocalStaticMethod.add, final] += value;
            }

            remove
            {
                // Should invoke this.
                link[_this.BaseMethod.remove, @base] -= value;
                // Should invoke this.
                link[_this.BaseMethod.remove, previous] -= value;
                // Should invoke this.
                link[_this.BaseMethod.remove, current] -= value;
                // Should invoke this.
                link[_this.BaseMethod.remove, final] -= value;

                // Should invoke current type.
                link[_static.Target.BaseStaticMethod.remove, @base] -= value;
                // Should invoke current type.
                link[_static.Target.BaseStaticMethod.remove, previous] -= value;
                // Should invoke current type.
                link[_static.Target.BaseStaticMethod.remove, current] -= value;
                // Should invoke current type.
                link[_static.Target.BaseStaticMethod.remove, final] -= value;

                // Should invoke base.
                link[_this.BaseVirtualMethod.remove, @base] -= value;
                // Should invoke base.
                link[_this.BaseVirtualMethod.remove, previous] -= value;
                // Should invoke base.
                link[_this.BaseVirtualMethod.remove, current] -= value;
                // Should invoke this.
                link[_this.BaseVirtualMethod.remove, final] -= value;

                // Should invoke _Source.
                link[_this.BaseVirtualOverriddenMethod.remove, @base] -= value;
                // Should invoke _Source.
                link[_this.BaseVirtualOverriddenMethod.remove, previous] -= value;
                // Should invoke _Source.
                link[_this.BaseVirtualOverriddenMethod.remove, current] -= value;
                // Should invoke this.
                link[_this.BaseVirtualOverriddenMethod.remove, final] -= value;

                // Should invoke _Source.
                link[_this.BaseVirtualHiddenMethod.remove, @base] -= value;
                // Should invoke _Source.
                link[_this.BaseVirtualHiddenMethod.remove, previous] -= value;
                // Should invoke _Source.
                link[_this.BaseVirtualHiddenMethod.remove, current] -= value;
                // Should invoke this.
                link[_this.BaseVirtualHiddenMethod.remove, final] -= value;

                // Should invoke this.
                link[_this.BaseHiddenMethod.remove, @base] -= value;
                // Should invoke this.
                link[_this.BaseHiddenMethod.remove, previous] -= value;
                // Should invoke this.
                link[_this.BaseHiddenMethod.remove, current] -= value;
                // Should invoke this.
                link[_this.BaseHiddenMethod.remove, final] -= value;

                // Should invoke current type.
                link[_static.Target.BaseStaticHiddenMethod.remove, @base] -= value;
                // Should invoke current type.
                link[_static.Target.BaseStaticHiddenMethod.remove, previous] -= value;
                // Should invoke current type.
                link[_static.Target.BaseStaticHiddenMethod.remove, current] -= value;
                // Should invoke current type.
                link[_static.Target.BaseStaticHiddenMethod.remove, final] -= value;

                // Should invoke this.
                link[_this.LocalMethod.remove, @base] -= value;
                // Should invoke this.
                link[_this.LocalMethod.remove, previous] -= value;
                // Should invoke this.
                link[_this.LocalMethod.remove, current] -= value;
                // Should invoke this.
                link[_this.LocalMethod.remove, final] -= value;

                // Should invoke _Source.
                link[_this.LocalVirtualMethod.remove, @base] -= value;
                // Should invoke _Source.
                link[_this.LocalVirtualMethod.remove, previous] -= value;
                // Should invoke _Source.
                link[_this.LocalVirtualMethod.remove, current] -= value;
                // Should invoke this.
                link[_this.LocalVirtualMethod.remove, final] -= value;

                // Should invoke current type.
                link[_static.Target.LocalStaticMethod.remove, @base] -= value;
                // Should invoke current type.
                link[_static.Target.LocalStaticMethod.remove, previous] -= value;
                // Should invoke current type.
                link[_static.Target.LocalStaticMethod.remove, current] -= value;
                // Should invoke current type.
                link[_static.Target.LocalStaticMethod.remove, final] -= value;
            }
        }
    }
}