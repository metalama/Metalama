using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS0067

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictOverrideBaseNonVirtual_EventField
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override )]
        public event EventHandler? BaseEvent;

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public static event EventHandler? BaseEvent_Static;
    }

    internal class BaseClass
    {
        public event EventHandler? BaseEvent
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

        public static event EventHandler? BaseEvent_Static
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