// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.BackingFieldAccess
{
    /*
     * Tests that accessors with field references work when overridden.
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

    // <target>
    internal class TargetClass
    {
        [Override]
        public int Property
        {
            get
            {
                Console.WriteLine( "This is the original getter.");
                return field;
            }

            set
            {
                Console.WriteLine( "This is the original setter.");
                field = value;
            }
        }

        [Override]
        public static int StaticProperty
        {
            get
            {
                Console.WriteLine( "This is the original getter.");
                return field;
            }

            set
            {
                Console.WriteLine( "This is the original setter.");
                field = value;
            }
        }
    }
}