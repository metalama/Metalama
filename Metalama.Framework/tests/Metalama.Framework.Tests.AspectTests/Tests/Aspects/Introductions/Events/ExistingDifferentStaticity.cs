using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingDifferentStaticity
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public static event EventHandler ExistingEvent
        {
            add
            {
                Console.WriteLine( "This is introduced event." );
            }

            remove
            {
                Console.WriteLine( "This is introduced event." );
            }
        }

        [Introduce]
        public event EventHandler ExistingEvent_Static
        {
            add
            {
                Console.WriteLine( "This is introduced event." );
            }

            remove
            {
                Console.WriteLine( "This is introduced event." );
            }
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public event EventHandler ExistingEvent
        {
            add
            {
                Console.WriteLine( "This is original event." );
            }

            remove
            {
                Console.WriteLine( "This is original event." );
            }
        }

        public static event EventHandler ExistingEvent_Static
        {
            add
            {
                Console.WriteLine( "This is original event." );
            }

            remove
            {
                Console.WriteLine( "This is original event." );
            }
        }
    }
}