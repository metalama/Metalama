// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.ComponentModel;

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_GetOnly_Trivias
{
    /*
     * Tests that trivia (doc comments), type name syntax, and attributes are preserved when overriding get-only auto properties,
     * including abstract/virtual base property overrides where the PrivatelyWriteableProperty pattern is used.
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

        public virtual int VirtualBaseProperty => 0;
    }

    // <target>
    internal class TargetClass : BaseClass
    {
        // Comment before property.
        /// <summary>
        /// Gets the property value.
        /// </summary>
        [Override]
        public int Property { get; }

        /// <summary>
        /// Gets the static property value.
        /// </summary>
        [Override]
        public static int StaticProperty { get; }

        /// <summary>
        /// Gets the initializer property value.
        /// </summary>
        [Override]
        [Description( "An initializer property" )]
        public int InitializerProperty { get; } = 42;

        /// <summary>
        /// Gets the abstract base property value.
        /// </summary>
        [Override]
        public override int AbstractBaseProperty { get; }

        /// <summary>
        /// Gets the virtual base property value.
        /// </summary>
        [Override]
        public override int VirtualBaseProperty { get; }

        public TargetClass()
        {
            Property = 27;
            InitializerProperty = 27;
            AbstractBaseProperty = 27;
            VirtualBaseProperty = 27;
        }

        static TargetClass()
        {
            StaticProperty = 27;
        }
    }
}
