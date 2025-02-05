using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS0067

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictOverrideDifferentEventType_EventField
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override )]
        public event EventHandler? ExistingEvent;
    }

    internal class BaseClass
    {
        public virtual event UnhandledExceptionEventHandler? ExistingEvent
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