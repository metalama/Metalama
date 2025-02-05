using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS0067

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictOverrideBaseSealed_EventField
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override )]
        public event EventHandler? ExistingEvent;
    }

    internal class BaseClass
    {
        public virtual event EventHandler? ExistingEvent
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

    internal class DerivedClass : BaseClass
    {
        public sealed override event EventHandler? ExistingEvent
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
    internal class TargetClass : DerivedClass { }
}