using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictFail
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Fail )]
        public event EventHandler ExistingEvent
        {
            add
            {
                Console.WriteLine( "This is introduced event." );
                meta.Proceed();
            }

            remove
            {
                Console.WriteLine( "This is introduced event." );
                meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.Fail )]
        public static event EventHandler ExistingEvent_Static
        {
            add
            {
                Console.WriteLine( "This is introduced event." );
                meta.Proceed();
            }

            remove
            {
                Console.WriteLine( "This is introduced event." );
                meta.Proceed();
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