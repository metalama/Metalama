using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictNew_Error
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.New )]
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

        [Introduce( WhenExists = OverrideStrategy.New )]
        public event EventHandler ExistingVirtualEvent
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

        public virtual event EventHandler ExistingVirtualEvent
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