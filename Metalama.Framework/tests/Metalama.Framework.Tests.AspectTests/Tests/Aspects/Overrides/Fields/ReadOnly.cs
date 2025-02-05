using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.ReadOnly
{
    /*
     * Tests that overriding of readonly fields is possible, including introduced readonly fields.
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
        public readonly int ReadOnlyField;

        [Override]
        public static readonly int StaticReadOnlyField;

        [Override]
        public readonly int InitializerReadOnlyField = 42;

        [Override]
        public static readonly int StaticInitializerReadOnlyField = 42;

        static TargetClass()
        {
            StaticReadOnlyField = 42;
            StaticInitializerReadOnlyField = 27;
        }

        public TargetClass()
        {
            ReadOnlyField = 42;
            InitializerReadOnlyField = 27;
        }

        public int __Init
        {
            init
            {
                // Overridden read-only fields should be accessible from init accessors.
                ReadOnlyField = 13;
                InitializerReadOnlyField = 13;
            }
        }
    }
}