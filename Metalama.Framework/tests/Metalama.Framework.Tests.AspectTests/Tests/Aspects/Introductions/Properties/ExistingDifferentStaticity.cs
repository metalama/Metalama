using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.ExistingDifferentStaticity
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public static int ExistingProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return 42;
            }
        }

        [Introduce]
        public int ExistingProperty_Static
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return 42;
            }
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public int ExistingProperty
        {
            get
            {
                return 0;
            }
        }

        public static int ExistingProperty_Static
        {
            get
            {
                return 0;
            }
        }
    }
}