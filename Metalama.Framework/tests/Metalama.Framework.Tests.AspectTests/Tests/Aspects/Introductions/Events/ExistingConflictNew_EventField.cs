using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS0067

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictNew_EventField
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.New )]
        public event EventHandler? BaseClassEvent;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static event EventHandler? BaseClassEvent_Static;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public event EventHandler? HiddenBaseClassEvent;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static event EventHandler? HiddenBaseClassEvent_Static;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public event EventHandler? HiddenBaseClassVirtualEvent;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public event EventHandler? HiddenVirtualBaseClassVirtualEvent;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public event EventHandler? BaseClassVirtualEvent;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public event EventHandler? BaseClassVirtualSealedEvent;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public event EventHandler? BaseClassVirtualOverridenEvent;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public event EventHandler? BaseClassAbstractEvent;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public event EventHandler? BaseClassAbstractSealedEvent;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public event EventHandler? DerivedClassEvent;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static event EventHandler? DerivedClassEvent_Static;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public event EventHandler? DerivedClassVirtualEvent;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public event EventHandler? DerivedClassVirtualSealedEvent;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public event EventHandler? NonExistentEvent;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static event EventHandler? NonExistentEvent_Static;
    }

    internal abstract class BaseClass
    {
        public event EventHandler? BaseClassEvent
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

        public static event EventHandler? BaseClassEvent_Static
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

        public event EventHandler? HiddenBaseClassEvent
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

        public static event EventHandler? HiddenBaseClassEvent_Static
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

        public event EventHandler? HiddenBaseClassVirtualEvent
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

        public event EventHandler? HiddenVirtualBaseClassVirtualEvent
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

        public virtual event EventHandler? BaseClassVirtualEvent
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

        public virtual event EventHandler? BaseClassVirtualSealedEvent
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

        public virtual event EventHandler? BaseClassVirtualOverridenEvent
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

        public abstract event EventHandler? BaseClassAbstractEvent;

        public abstract event EventHandler? BaseClassAbstractSealedEvent;
    }

    internal class DerivedClass : BaseClass
    {
        public new event EventHandler? HiddenBaseClassEvent
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

        public new static event EventHandler? HiddenBaseClassEvent_Static
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

        public new event EventHandler? HiddenBaseClassVirtualEvent
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

        public new virtual event EventHandler? HiddenVirtualBaseClassVirtualEvent
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

        public sealed override event EventHandler? BaseClassVirtualSealedEvent
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

        public override event EventHandler? BaseClassVirtualOverridenEvent
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

        public override event EventHandler? BaseClassAbstractEvent
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

        public sealed override event EventHandler? BaseClassAbstractSealedEvent
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

        public event EventHandler? DerivedClassEvent
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

        public static event EventHandler? DerivedClassEvent_Static
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

        public virtual event EventHandler? DerivedClassVirtualEvent
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

        public virtual event EventHandler? DerivedClassVirtualSealedEvent
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