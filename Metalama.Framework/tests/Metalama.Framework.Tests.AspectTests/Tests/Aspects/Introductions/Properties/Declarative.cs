using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.Declarative
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public int IntroducedProperty_Auto { get; set; }

        [Introduce]
        public int IntroducedProperty_Auto_Initializer { get; set; } = 42;

        [Introduce]
        public int IntroducedProperty_Auto_GetOnly { get; }

        [Introduce]
        public int IntroducedProperty_Auto_GetOnly_Initializer { get; } = 42;

        [Introduce]
        public static int IntroducedProperty_Auto_Static { get; set; }

        [Introduce]
        public int IntroducedProperty_Accessors
        {
            get
            {
                Console.WriteLine( "Get" );

                return 42;
            }

            set
            {
                Console.WriteLine( value );
            }
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}