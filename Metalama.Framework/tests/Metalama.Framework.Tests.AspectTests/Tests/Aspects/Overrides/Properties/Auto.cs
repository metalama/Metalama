using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto
{
    /*
     * Tests a single OverrideProperty aspect on auto properties.
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
        public int Property { get; set; }

        [Override]
        public static int StaticProperty { get; set; }

        [Override]
        public int PropertyInitOnly { get; init; }

        public int __Init
        {
            init
            {
                // Init-only setter should be accessible from other init-only setters.
                PropertyInitOnly = 42;
            }
        }
    }
}