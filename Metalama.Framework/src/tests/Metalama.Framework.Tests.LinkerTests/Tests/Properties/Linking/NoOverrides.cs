// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

        public static new int BaseStaticHiddenMethod
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
                _ = Link(This.BaseMethod.get, Api.Base);
                // Should invoke this.
                _ = Link(This.BaseMethod.get, Previous );
                // Should invoke this.
                _ = Link(This.BaseMethod.get, Current);
                // Should invoke this.
                _ = Link(This.BaseMethod.get, Final);

                // Should invoke current type.
                _ = Link(Static.Target.BaseStaticMethod.get, Api.Base);
                // Should invoke current type.
                _ = Link(Static.Target.BaseStaticMethod.get, Previous );
                // Should invoke current type.
                _ = Link(Static.Target.BaseStaticMethod.get, Current);
                // Should invoke current type.
                _ = Link(Static.Target.BaseStaticMethod.get, Final);

                // Should invoke base.
                _ = Link(This.BaseVirtualMethod.get, Api.Base);
                // Should invoke base.
                _ = Link(This.BaseVirtualMethod.get, Previous );
                // Should invoke base.
                _ = Link(This.BaseVirtualMethod.get, Current);
                // Should invoke this.
                _ = Link(This.BaseVirtualMethod.get, Final);

                // Should invoke _Source.
                _ = Link(This.BaseVirtualOverriddenMethod.get, Api.Base);
                // Should invoke _Source.
                _ = Link(This.BaseVirtualOverriddenMethod.get, Previous );
                // Should invoke _Source.
                _ = Link(This.BaseVirtualOverriddenMethod.get, Current);
                // Should invoke this.
                _ = Link(This.BaseVirtualOverriddenMethod.get, Final);

                // Should invoke _Source.
                _ = Link(This.BaseVirtualHiddenMethod.get, Api.Base);
                // Should invoke _Source.
                _ = Link(This.BaseVirtualHiddenMethod.get, Previous );
                // Should invoke _Source.
                _ = Link(This.BaseVirtualHiddenMethod.get, Current);
                // Should invoke this.
                _ = Link(This.BaseVirtualHiddenMethod.get, Final);

                // Should invoke this.
                _ = Link(This.BaseHiddenMethod.get, Api.Base);
                // Should invoke this.
                _ = Link(This.BaseHiddenMethod.get, Previous );
                // Should invoke this.
                _ = Link(This.BaseHiddenMethod.get, Current);
                // Should invoke this.
                _ = Link(This.BaseHiddenMethod.get, Final);

                // Should invoke current type.
                _ = Link(Static.Target.BaseStaticHiddenMethod.get, Api.Base);
                // Should invoke current type.
                _ = Link(Static.Target.BaseStaticHiddenMethod.get, Previous );
                // Should invoke current type.
                _ = Link(Static.Target.BaseStaticHiddenMethod.get, Current);
                // Should invoke current type.
                _ = Link(Static.Target.BaseStaticHiddenMethod.get, Final);

                // Should invoke this.
                _ = Link(This.LocalMethod.get, Api.Base);
                // Should invoke this.
                _ = Link(This.LocalMethod.get, Previous );
                // Should invoke this.
                _ = Link(This.LocalMethod.get, Current);
                // Should invoke this.
                _ = Link(This.LocalMethod.get, Final);

                // Should invoke _Source.
                _ = Link(This.LocalVirtualMethod.get, Api.Base);
                // Should invoke _Source.
                _ = Link(This.LocalVirtualMethod.get, Previous );
                // Should invoke _Source.
                _ = Link(This.LocalVirtualMethod.get, Current);
                // Should invoke this.
                _ = Link(This.LocalVirtualMethod.get, Final);

                // Should invoke current type.
                _ = Link(Static.Target.LocalStaticMethod.get, Api.Base);
                // Should invoke current type.
                _ = Link(Static.Target.LocalStaticMethod.get, Previous );
                // Should invoke current type.
                _ = Link(Static.Target.LocalStaticMethod.get, Current);
                // Should invoke current type.
                _ = Link(Static.Target.LocalStaticMethod.get, Final);

                return 42;
            }

            set
            {
                // Should invoke this.
                Link[This.BaseMethod.set, Api.Base] = value;
                // Should invoke this.
                Link[This.BaseMethod.set, Previous] = value;
                // Should invoke this.
                Link[This.BaseMethod.set, Current] = value;
                // Should invoke this.
                Link[This.BaseMethod.set, Final] = value;

                // Should invoke current type.
                Link[Static.Target.BaseStaticMethod.set, Api.Base] = value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticMethod.set, Previous] = value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticMethod.set, Current] = value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticMethod.set, Final] = value;

                // Should invoke base.
                Link[This.BaseVirtualMethod.set, Api.Base] = value;
                // Should invoke base.
                Link[This.BaseVirtualMethod.set, Previous] = value;
                // Should invoke base.
                Link[This.BaseVirtualMethod.set, Current] = value;
                // Should invoke this.
                Link[This.BaseVirtualMethod.set, Final] = value;

                // Should invoke _Source.
                Link[This.BaseVirtualOverriddenMethod.set, Api.Base] = value;
                // Should invoke _Source.
                Link[This.BaseVirtualOverriddenMethod.set, Previous] = value;
                // Should invoke _Source.
                Link[This.BaseVirtualOverriddenMethod.set, Current] = value;
                // Should invoke this.
                Link[This.BaseVirtualOverriddenMethod.set, Final] = value;

                // Should invoke _Source.
                Link[This.BaseVirtualHiddenMethod.set, Api.Base] = value;
                // Should invoke _Source.
                Link[This.BaseVirtualHiddenMethod.set, Previous] = value;
                // Should invoke _Source.
                Link[This.BaseVirtualHiddenMethod.set, Current] = value;
                // Should invoke this.
                Link[This.BaseVirtualHiddenMethod.set, Final] = value;

                // Should invoke this.
                Link[This.BaseHiddenMethod.set, Api.Base] = value;
                // Should invoke this.
                Link[This.BaseHiddenMethod.set, Previous] = value;
                // Should invoke this.
                Link[This.BaseHiddenMethod.set, Current] = value;
                // Should invoke this.
                Link[This.BaseHiddenMethod.set, Final] = value;

                // Should invoke current type.
                Link[Static.Target.BaseStaticHiddenMethod.set, Api.Base] = value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticHiddenMethod.set, Previous] = value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticHiddenMethod.set, Current] = value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticHiddenMethod.set, Final] = value;

                // Should invoke this.
                Link[This.LocalMethod.set, Api.Base] = value;
                // Should invoke this.
                Link[This.LocalMethod.set, Previous] = value;
                // Should invoke this.
                Link[This.LocalMethod.set, Current] = value;
                // Should invoke this.
                Link[This.LocalMethod.set, Final] = value;

                // Should invoke _Source.
                Link[This.LocalVirtualMethod.set, Api.Base] = value;
                // Should invoke _Source.
                Link[This.LocalVirtualMethod.set, Previous] = value;
                // Should invoke _Source.
                Link[This.LocalVirtualMethod.set, Current] = value;
                // Should invoke this.
                Link[This.LocalVirtualMethod.set, Final] = value;

                // Should invoke current type.
                Link[Static.Target.LocalStaticMethod.set, Api.Base] = value;
                // Should invoke current type.
                Link[Static.Target.LocalStaticMethod.set, Previous] = value;
                // Should invoke current type.
                Link[Static.Target.LocalStaticMethod.set, Current] = value;
                // Should invoke current type.
                Link[Static.Target.LocalStaticMethod.set, Final] = value;
            }
        }
    }
}