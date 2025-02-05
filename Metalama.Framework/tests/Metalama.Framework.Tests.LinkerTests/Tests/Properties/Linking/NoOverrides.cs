using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Properties.Linking.NoOverrides
{
    public class Base
    {
        public int BaseMethod
        {
            get
            {
                return 42;
            }
            set
            {

            }
        }

        public static int BaseStaticMethod
        {
            get
            {
                return 42;
            }
            set
            {
            }
        }

        public virtual int BaseVirtualMethod
        {
            get
            {
                return 42;
            }
            set
            {
            }
        }

        public virtual int BaseVirtualOverriddenMethod
        {
            get
            {
                return 42;
            }
            set
            {
            }
        }

        public virtual int BaseVirtualHiddenMethod
        {
            get
            {
                return 42;
            }
            set
            {
            }
        }

        public int BaseHiddenMethod
        {
            get
            {
                return 42;
            }
            set
            {

            }
        }

        public static int BaseStaticHiddenMethod
        {
            get
            {
                return 42;
            }
            set
            {
            }
        }
    }

    [PseudoLayerOrder("TestAspect")]
    // <target>
    public class Target : Base
    {

        public override int BaseVirtualOverriddenMethod
        {
            get
            {
                return 42;
            }
            set
            {
            }
        }

        public new virtual int BaseVirtualHiddenMethod
        {
            get
            {
                return 42;
            }
            set
            {
            }
        }

        public new int BaseHiddenMethod
        {
            get
            {
                return 42;
            }
            set
            {
            }
        }

        public new static int BaseStaticHiddenMethod
        {
            get
            {
                return 42;
            }
            set
            {
            }
        }

        public int LocalMethod
        {
            get
            {
                return 42;
            }
            set
            {

            }
        }

        public virtual int LocalVirtualMethod
        {
            get
            {
                return 42;
            }
            set
            {
            }
        }

        public static int LocalStaticMethod
        {
            get
            {
                return 42;
            }
            set
            {
            }
        }

        public int Foo
        {
            get
            {
                return 42;
            }
            set
            {

            }
        }

        [PseudoOverride(nameof(Foo), "TestAspect")]
        public int Foo_Override
        {
            get
            {
                // Should invoke this.
                _ = link(_this.BaseMethod.get, @base);
                // Should invoke this.
                _ = link(_this.BaseMethod.get, previous);
                // Should invoke this.
                _ = link(_this.BaseMethod.get, current);
                // Should invoke this.
                _ = link(_this.BaseMethod.get, final);

                // Should invoke current type.
                _ = link(_static.Target.BaseStaticMethod.get, @base);
                // Should invoke current type.
                _ = link(_static.Target.BaseStaticMethod.get, previous);
                // Should invoke current type.
                _ = link(_static.Target.BaseStaticMethod.get, current);
                // Should invoke current type.
                _ = link(_static.Target.BaseStaticMethod.get, final);

                // Should invoke base.
                _ = link(_this.BaseVirtualMethod.get, @base);
                // Should invoke base.
                _ = link(_this.BaseVirtualMethod.get, previous);
                // Should invoke base.
                _ = link(_this.BaseVirtualMethod.get, current);
                // Should invoke this.
                _ = link(_this.BaseVirtualMethod.get, final);

                // Should invoke _Source.
                _ = link(_this.BaseVirtualOverriddenMethod.get, @base);
                // Should invoke _Source.
                _ = link(_this.BaseVirtualOverriddenMethod.get, previous);
                // Should invoke _Source.
                _ = link(_this.BaseVirtualOverriddenMethod.get, current);
                // Should invoke this.
                _ = link(_this.BaseVirtualOverriddenMethod.get, final);

                // Should invoke _Source.
                _ = link(_this.BaseVirtualHiddenMethod.get, @base);
                // Should invoke _Source.
                _ = link(_this.BaseVirtualHiddenMethod.get, previous);
                // Should invoke _Source.
                _ = link(_this.BaseVirtualHiddenMethod.get, current);
                // Should invoke this.
                _ = link(_this.BaseVirtualHiddenMethod.get, final);

                // Should invoke this.
                _ = link(_this.BaseHiddenMethod.get, @base);
                // Should invoke this.
                _ = link(_this.BaseHiddenMethod.get, previous);
                // Should invoke this.
                _ = link(_this.BaseHiddenMethod.get, current);
                // Should invoke this.
                _ = link(_this.BaseHiddenMethod.get, final);

                // Should invoke current type.
                _ = link(_static.Target.BaseStaticHiddenMethod.get, @base);
                // Should invoke current type.
                _ = link(_static.Target.BaseStaticHiddenMethod.get, previous);
                // Should invoke current type.
                _ = link(_static.Target.BaseStaticHiddenMethod.get, current);
                // Should invoke current type.
                _ = link(_static.Target.BaseStaticHiddenMethod.get, final);

                // Should invoke this.
                _ = link(_this.LocalMethod.get, @base);
                // Should invoke this.
                _ = link(_this.LocalMethod.get, previous);
                // Should invoke this.
                _ = link(_this.LocalMethod.get, current);
                // Should invoke this.
                _ = link(_this.LocalMethod.get, final);

                // Should invoke _Source.
                _ = link(_this.LocalVirtualMethod.get, @base);
                // Should invoke _Source.
                _ = link(_this.LocalVirtualMethod.get, previous);
                // Should invoke _Source.
                _ = link(_this.LocalVirtualMethod.get, current);
                // Should invoke this.
                _ = link(_this.LocalVirtualMethod.get, final);

                // Should invoke current type.
                _ = link(_static.Target.LocalStaticMethod.get, @base);
                // Should invoke current type.
                _ = link(_static.Target.LocalStaticMethod.get, previous);
                // Should invoke current type.
                _ = link(_static.Target.LocalStaticMethod.get, current);
                // Should invoke current type.
                _ = link(_static.Target.LocalStaticMethod.get, final);

                return 42;
            }

            set
            {
                // Should invoke this.
                link[_this.BaseMethod.set, @base] = value;
                // Should invoke this.
                link[_this.BaseMethod.set, previous] = value;
                // Should invoke this.
                link[_this.BaseMethod.set, current] = value;
                // Should invoke this.
                link[_this.BaseMethod.set, final] = value;

                // Should invoke current type.
                link[_static.Target.BaseStaticMethod.set, @base] = value;
                // Should invoke current type.
                link[_static.Target.BaseStaticMethod.set, previous] = value;
                // Should invoke current type.
                link[_static.Target.BaseStaticMethod.set, current] = value;
                // Should invoke current type.
                link[_static.Target.BaseStaticMethod.set, final] = value;

                // Should invoke base.
                link[_this.BaseVirtualMethod.set, @base] = value;
                // Should invoke base.
                link[_this.BaseVirtualMethod.set, previous] = value;
                // Should invoke base.
                link[_this.BaseVirtualMethod.set, current] = value;
                // Should invoke this.
                link[_this.BaseVirtualMethod.set, final] = value;

                // Should invoke _Source.
                link[_this.BaseVirtualOverriddenMethod.set, @base] = value;
                // Should invoke _Source.
                link[_this.BaseVirtualOverriddenMethod.set, previous] = value;
                // Should invoke _Source.
                link[_this.BaseVirtualOverriddenMethod.set, current] = value;
                // Should invoke this.
                link[_this.BaseVirtualOverriddenMethod.set, final] = value;

                // Should invoke _Source.
                link[_this.BaseVirtualHiddenMethod.set, @base] = value;
                // Should invoke _Source.
                link[_this.BaseVirtualHiddenMethod.set, previous] = value;
                // Should invoke _Source.
                link[_this.BaseVirtualHiddenMethod.set, current] = value;
                // Should invoke this.
                link[_this.BaseVirtualHiddenMethod.set, final] = value;

                // Should invoke this.
                link[_this.BaseHiddenMethod.set, @base] = value;
                // Should invoke this.
                link[_this.BaseHiddenMethod.set, previous] = value;
                // Should invoke this.
                link[_this.BaseHiddenMethod.set, current] = value;
                // Should invoke this.
                link[_this.BaseHiddenMethod.set, final] = value;

                // Should invoke current type.
                link[_static.Target.BaseStaticHiddenMethod.set, @base] = value;
                // Should invoke current type.
                link[_static.Target.BaseStaticHiddenMethod.set, previous] = value;
                // Should invoke current type.
                link[_static.Target.BaseStaticHiddenMethod.set, current] = value;
                // Should invoke current type.
                link[_static.Target.BaseStaticHiddenMethod.set, final] = value;

                // Should invoke this.
                link[_this.LocalMethod.set, @base] = value;
                // Should invoke this.
                link[_this.LocalMethod.set, previous] = value;
                // Should invoke this.
                link[_this.LocalMethod.set, current] = value;
                // Should invoke this.
                link[_this.LocalMethod.set, final] = value;

                // Should invoke _Source.
                link[_this.LocalVirtualMethod.set, @base] = value;
                // Should invoke _Source.
                link[_this.LocalVirtualMethod.set, previous] = value;
                // Should invoke _Source.
                link[_this.LocalVirtualMethod.set, current] = value;
                // Should invoke this.
                link[_this.LocalVirtualMethod.set, final] = value;

                // Should invoke current type.
                link[_static.Target.LocalStaticMethod.set, @base] = value;
                // Should invoke current type.
                link[_static.Target.LocalStaticMethod.set, previous] = value;
                // Should invoke current type.
                link[_static.Target.LocalStaticMethod.set, current] = value;
                // Should invoke current type.
                link[_static.Target.LocalStaticMethod.set, final] = value;
            }
        }
    }
}