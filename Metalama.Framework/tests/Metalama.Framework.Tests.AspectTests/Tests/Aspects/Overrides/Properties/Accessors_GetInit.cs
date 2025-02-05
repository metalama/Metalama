using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Accessors_GetInit
{
    /*
     * Tests a single OverrideProperty aspect on get&init properties.
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
                Console.WriteLine( "This is the original getter." );

                return 42;
            }

            init
            {
                Console.WriteLine( $"This is the original setter, setting {value}." );
            }
        }

        [Override]
        private int PrivateProperty
        {
            get
            {
                Console.WriteLine( "This is the original getter." );

                return 42;
            }

            init
            {
                Console.WriteLine( $"This is the original setter, setting {value}." );
            }
        }

        [Override]
        public int RestrictedGetProperty
        {
            private get
            {
                Console.WriteLine( "This is the original getter." );

                return 42;
            }

            init
            {
                Console.WriteLine( $"This is the original setter, setting {value}." );
            }
        }

        [Override]
        protected int ProtectedRestrictedGetProperty
        {
            private get
            {
                Console.WriteLine( "This is the original getter." );

                return 42;
            }

            init
            {
                Console.WriteLine( $"This is the original setter, setting {value}." );
            }
        }

        [Override]
        public int RestrictedSetProperty
        {
            get
            {
                Console.WriteLine( "This is the original getter." );

                return 42;
            }

            private init
            {
                Console.WriteLine( $"This is the original setter, setting {value}." );
            }
        }

        [Override]
        protected int ProtectedestrictedSetProperty
        {
            get
            {
                Console.WriteLine( "This is the original getter." );

                return 42;
            }

            private init
            {
                Console.WriteLine( $"This is the original setter, setting {value}." );
            }
        }

        [Override]
        public int GetExpressionProperty
        {
            get => 42;

            init
            {
                Console.WriteLine( $"This is the original setter, setting {value}." );
            }
        }

        [Override]
        public int InitExpressionProperty
        {
            get
            {
                Console.WriteLine( "This is the original getter." );

                return 42;
            }

            init => Console.WriteLine( $"This is the original setter, setting {value}." );
        }
    }
}