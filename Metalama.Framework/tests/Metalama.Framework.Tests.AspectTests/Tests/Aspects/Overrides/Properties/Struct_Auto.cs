using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Struct_Auto
{
    /*
     * Tests a single OverrideProperty aspect on auto properties in a struct.
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
    internal struct TargetStruct
    {
        [Override]
        public int Property { get; set; }

        [Override]
        public static int StaticProperty { get; set; }

        [Override]
        public int PropertyInitOnly { get; init; }

        [Override]
        public int StaticPropertyInitOnly { get; init; }
    }
}