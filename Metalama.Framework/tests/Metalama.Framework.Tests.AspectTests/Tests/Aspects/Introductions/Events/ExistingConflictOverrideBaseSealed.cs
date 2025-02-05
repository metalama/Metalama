using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictOverrideBaseSealed
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override )]
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
    }

    internal class BaseClass
    {
        public virtual event EventHandler ExistingEvent
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
        public sealed override event EventHandler ExistingEvent
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