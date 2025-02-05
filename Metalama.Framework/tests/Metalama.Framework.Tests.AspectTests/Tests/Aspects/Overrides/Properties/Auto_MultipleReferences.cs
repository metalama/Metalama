using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_MultipleReferences
{
    /*
     * Tests a single OverrideProperty aspect that references the backing storage multiple times.
     */

    public class OverrideAttribute : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get
            {
                Console.WriteLine( "This is the overridden getter." );

                if (meta.Target.Property.Value < 0)
                {
                    meta.Target.Property.Value = 0;
                }

                return meta.Target.Property.Value;
            }

            set
            {
                Console.WriteLine( "This is the overridden setter." );

                var current = meta.Target.Property.Value;

                meta.Target.Property.Value = current + 1;
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
    }
}