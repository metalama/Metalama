// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

        public static new event EventHandler BaseStaticHiddenMethod
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
                Link[This.BaseMethod.add, Api.Base] += value;
                // Should invoke this.
                Link[This.BaseMethod.add, Previous] += value;
                // Should invoke this.
                Link[This.BaseMethod.add, Current] += value;
                // Should invoke this.
                Link[This.BaseMethod.add, Final] += value;

                // Should invoke current type.
                Link[Static.Target.BaseStaticMethod.add, Api.Base] += value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticMethod.add, Previous] += value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticMethod.add, Current] += value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticMethod.add, Final] += value;

                // Should invoke base.
                Link[This.BaseVirtualMethod.add, Api.Base] += value;
                // Should invoke base.
                Link[This.BaseVirtualMethod.add, Previous] += value;
                // Should invoke base.
                Link[This.BaseVirtualMethod.add, Current] += value;
                // Should invoke this.
                Link[This.BaseVirtualMethod.add, Final] += value;

                // Should invoke _Source.
                Link[This.BaseVirtualOverriddenMethod.add, Api.Base] += value;
                // Should invoke _Source.
                Link[This.BaseVirtualOverriddenMethod.add, Previous] += value;
                // Should invoke _Source.
                Link[This.BaseVirtualOverriddenMethod.add, Current] += value;
                // Should invoke this.
                Link[This.BaseVirtualOverriddenMethod.add, Final] += value;

                // Should invoke _Source.
                Link[This.BaseVirtualHiddenMethod.add, Api.Base] += value;
                // Should invoke _Source.
                Link[This.BaseVirtualHiddenMethod.add, Previous] += value;
                // Should invoke _Source.
                Link[This.BaseVirtualHiddenMethod.add, Current] += value;
                // Should invoke this.
                Link[This.BaseVirtualHiddenMethod.add, Final] += value;

                // Should invoke this.
                Link[This.BaseHiddenMethod.add, Api.Base] += value;
                // Should invoke this.
                Link[This.BaseHiddenMethod.add, Previous] += value;
                // Should invoke this.
                Link[This.BaseHiddenMethod.add, Current] += value;
                // Should invoke this.
                Link[This.BaseHiddenMethod.add, Final] += value;

                // Should invoke current type.
                Link[Static.Target.BaseStaticHiddenMethod.add, Api.Base] += value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticHiddenMethod.add, Previous] += value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticHiddenMethod.add, Current] += value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticHiddenMethod.add, Final] += value;

                // Should invoke this.
                Link[This.LocalMethod.add, Api.Base] += value;
                // Should invoke this.
                Link[This.LocalMethod.add, Previous] += value;
                // Should invoke this.
                Link[This.LocalMethod.add, Current] += value;
                // Should invoke this.
                Link[This.LocalMethod.add, Final] += value;

                // Should invoke _Source.
                Link[This.LocalVirtualMethod.add, Api.Base] += value;
                // Should invoke _Source.
                Link[This.LocalVirtualMethod.add, Previous] += value;
                // Should invoke _Source.
                Link[This.LocalVirtualMethod.add, Current] += value;
                // Should invoke this.
                Link[This.LocalVirtualMethod.add, Final] += value;

                // Should invoke current type.
                Link[Static.Target.LocalStaticMethod.add, Api.Base] += value;
                // Should invoke current type.
                Link[Static.Target.LocalStaticMethod.add, Previous] += value;
                // Should invoke current type.
                Link[Static.Target.LocalStaticMethod.add, Current] += value;
                // Should invoke current type.
                Link[Static.Target.LocalStaticMethod.add, Final] += value;
            }

            remove
            {
                // Should invoke this.
                Link[This.BaseMethod.remove, Api.Base] -= value;
                // Should invoke this.
                Link[This.BaseMethod.remove, Previous] -= value;
                // Should invoke this.
                Link[This.BaseMethod.remove, Current] -= value;
                // Should invoke this.
                Link[This.BaseMethod.remove, Final] -= value;

                // Should invoke current type.
                Link[Static.Target.BaseStaticMethod.remove, Api.Base] -= value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticMethod.remove, Previous] -= value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticMethod.remove, Current] -= value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticMethod.remove, Final] -= value;

                // Should invoke base.
                Link[This.BaseVirtualMethod.remove, Api.Base] -= value;
                // Should invoke base.
                Link[This.BaseVirtualMethod.remove, Previous] -= value;
                // Should invoke base.
                Link[This.BaseVirtualMethod.remove, Current] -= value;
                // Should invoke this.
                Link[This.BaseVirtualMethod.remove, Final] -= value;

                // Should invoke _Source.
                Link[This.BaseVirtualOverriddenMethod.remove, Api.Base] -= value;
                // Should invoke _Source.
                Link[This.BaseVirtualOverriddenMethod.remove, Previous] -= value;
                // Should invoke _Source.
                Link[This.BaseVirtualOverriddenMethod.remove, Current] -= value;
                // Should invoke this.
                Link[This.BaseVirtualOverriddenMethod.remove, Final] -= value;

                // Should invoke _Source.
                Link[This.BaseVirtualHiddenMethod.remove, Api.Base] -= value;
                // Should invoke _Source.
                Link[This.BaseVirtualHiddenMethod.remove, Previous] -= value;
                // Should invoke _Source.
                Link[This.BaseVirtualHiddenMethod.remove, Current] -= value;
                // Should invoke this.
                Link[This.BaseVirtualHiddenMethod.remove, Final] -= value;

                // Should invoke this.
                Link[This.BaseHiddenMethod.remove, Api.Base] -= value;
                // Should invoke this.
                Link[This.BaseHiddenMethod.remove, Previous] -= value;
                // Should invoke this.
                Link[This.BaseHiddenMethod.remove, Current] -= value;
                // Should invoke this.
                Link[This.BaseHiddenMethod.remove, Final] -= value;

                // Should invoke current type.
                Link[Static.Target.BaseStaticHiddenMethod.remove, Api.Base] -= value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticHiddenMethod.remove, Previous] -= value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticHiddenMethod.remove, Current] -= value;
                // Should invoke current type.
                Link[Static.Target.BaseStaticHiddenMethod.remove, Final] -= value;

                // Should invoke this.
                Link[This.LocalMethod.remove, Api.Base] -= value;
                // Should invoke this.
                Link[This.LocalMethod.remove, Previous] -= value;
                // Should invoke this.
                Link[This.LocalMethod.remove, Current] -= value;
                // Should invoke this.
                Link[This.LocalMethod.remove, Final] -= value;

                // Should invoke _Source.
                Link[This.LocalVirtualMethod.remove, Api.Base] -= value;
                // Should invoke _Source.
                Link[This.LocalVirtualMethod.remove, Previous] -= value;
                // Should invoke _Source.
                Link[This.LocalVirtualMethod.remove, Current] -= value;
                // Should invoke this.
                Link[This.LocalVirtualMethod.remove, Final] -= value;

                // Should invoke current type.
                Link[Static.Target.LocalStaticMethod.remove, Api.Base] -= value;
                // Should invoke current type.
                Link[Static.Target.LocalStaticMethod.remove, Previous] -= value;
                // Should invoke current type.
                Link[Static.Target.LocalStaticMethod.remove, Current] -= value;
                // Should invoke current type.
                Link[Static.Target.LocalStaticMethod.remove, Final] -= value;
            }
        }
    }
}