using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.PropertyTemplate_Init
{
    /*
     * Tests a single OverrideProperty aspect on init-only properties.
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
            init
            {
                Console.WriteLine( $"This is the original setter, setting {value}." );
            }
        }

        [Override]
        private int PrivateProperty
        {
            init
            {
                Console.WriteLine( $"This is the original setter, setting {value}." );
            }
        }

        [Override]
        public int ExpressionProperty
        {
            init => Console.WriteLine( $"This is the original setter, setting {value}." );
        }

        [Override]
        private int PrivateExpressionProperty
        {
            init => Console.WriteLine( $"This is the original setter, setting {value}." );
        }
    }
}