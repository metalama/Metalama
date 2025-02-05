using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictOverrideBaseNonVirtual
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override )]
        public event EventHandler BaseEvent
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

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public static event EventHandler BaseEvent_Static
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

    internal class BaseClass
    {
        public event EventHandler BaseEvent
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

        public static event EventHandler BaseEvent_Static
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

    // <target>
    [Introduction]
    internal class TargetClass : BaseClass { }
}