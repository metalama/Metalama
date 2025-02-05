using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictNewStatic_Error
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.New )]
        public static event EventHandler ExistingEvent
        {
            add
            {
                meta.InsertComment( "Write that the code is original." );
                meta.Proceed();
            }

            remove
            {
                meta.InsertComment( "Write that the code is original." );
                meta.Proceed();
            }
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public static event EventHandler ExistingEvent
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