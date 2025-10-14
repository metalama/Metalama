// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.BackingFieldAccess_Hybrid
{
    /*
     * Tests that hybrid accessors (one using body with field references the other implicit) work.
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
        public int Property_ImplicitSet
        {
            get
            {
                Console.WriteLine( "This is the original getter.");
                return field;
            }

            set;
        }

        [Override]
        public int Property_ImplicitInit
        {
            get
            {
                Console.WriteLine( "This is the original getter." );
                return field;
            }

            init;
        }

        [Override]
        public int Property_ImplicitGet
        {
            get;

            set
            {
                Console.WriteLine( "This is the original setter." );
                field = value;
            }
        }

        [Override]
        public int Property_GetOnly
        {
            get
            {
                Console.WriteLine( "This is the original getter." );
                return field;
            }
        }

        [Override]
        public static int StaticProperty_Get
        {
            get
            {
                Console.WriteLine( "This is the original getter." );
                return field;
            }

            set;
        }

        [Override]
        public static int StaticProperty_Set
        {
            get;

            set
            {
                Console.WriteLine( "This is the original setter." );
                field = value;
            }
        }
    }
}

#endif