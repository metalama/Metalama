using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS0067

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictNew_EventField_Error
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.New )]
        public event EventHandler? ExistingEvent;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static event EventHandler? ExistingEvent_Static;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public event EventHandler? ExistingVirtualEvent;
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public event EventHandler? ExistingEvent
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

        public static event EventHandler? ExistingEvent_Static
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

        public virtual event EventHandler? ExistingVirtualEvent
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