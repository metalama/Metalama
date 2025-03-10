// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_GetOnly
{
    /*
     * Tests a single OverrideProperty aspect on get-only auto properties, including introduced get-only auto properties.
     */

    public class OverrideAttribute : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get
            {
                Console.WriteLine( "This is the overridden getter." );

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "This is the overridden setter." );
                meta.Proceed();
            }
        }
    }

    internal abstract class BaseClass
    {
        public abstract int AbstractBaseProperty { get; }

        public abstract int AbstractBaseInitializerProperty { get; }

        public virtual int VirtualBaseProperty => 0;

        public virtual int VirtualBaseInitializerProperty => 0;
    }

    // <target>
    internal class TargetClass : BaseClass
    {
        [Override]
        public int Property { get; }

        [Override]
        public static int StaticProperty { get; }

        [Override]
        public int InitializerProperty { get; } = 42;

        [Override]
        public static int StaticInitializerProperty { get; } = 42;

        [Override]
        public override int AbstractBaseProperty { get; }

        [Override]
        public override int AbstractBaseInitializerProperty { get; } = 42;

        [Override]
        public override int VirtualBaseProperty { get; }

        [Override]
        public override int VirtualBaseInitializerProperty { get; } = 42;

        public TargetClass()
        {
            Property = 27;
            InitializerProperty = 27;
            AbstractBaseProperty = 27;
            AbstractBaseInitializerProperty = 27;
            VirtualBaseProperty = 27;
            VirtualBaseInitializerProperty = 27;
        }

        static TargetClass()
        {
            StaticProperty = 27;
            StaticInitializerProperty = 27;
        }
    }
}