using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictNew
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.New )]
        public event EventHandler BaseClassEvent
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
        public event EventHandler HiddenBaseClassEvent
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
        public event EventHandler HiddenBaseClassVirtualEvent
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
        public event EventHandler HiddenVirtualBaseClassVirtualEvent
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
        public event EventHandler BaseClassVirtualEvent
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
        public event EventHandler BaseClassVirtualSealedEvent
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
        public event EventHandler BaseClassVirtualOverridenEvent
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
        public event EventHandler BaseClassAbstractEvent
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
        public event EventHandler BaseClassAbstractSealedEvent
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
        public event EventHandler DerivedClassEvent
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
        public event EventHandler DerivedClassVirtualEvent
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
        public event EventHandler DerivedClassVirtualSealedEvent
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
        public event EventHandler NonExistentEvent
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

    internal abstract class BaseClass
    {
        public event EventHandler BaseClassEvent
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

        public event EventHandler HiddenBaseClassEvent
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

        public event EventHandler HiddenBaseClassVirtualEvent
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

        public event EventHandler HiddenVirtualBaseClassVirtualEvent
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

        public virtual event EventHandler BaseClassVirtualEvent
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

        public virtual event EventHandler BaseClassVirtualSealedEvent
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

        public virtual event EventHandler BaseClassVirtualOverridenEvent
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

        public abstract event EventHandler BaseClassAbstractEvent;

        public abstract event EventHandler BaseClassAbstractSealedEvent;
    }

    internal class DerivedClass : BaseClass
    {
        public new event EventHandler HiddenBaseClassEvent
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

        public new event EventHandler HiddenBaseClassVirtualEvent
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

        public new virtual event EventHandler HiddenVirtualBaseClassVirtualEvent
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

        public sealed override event EventHandler BaseClassVirtualSealedEvent
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

        public override event EventHandler BaseClassVirtualOverridenEvent
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

        public override event EventHandler BaseClassAbstractEvent
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

        public sealed override event EventHandler BaseClassAbstractSealedEvent
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

        public event EventHandler DerivedClassEvent
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

        public virtual event EventHandler DerivedClassVirtualEvent
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

        public virtual event EventHandler DerivedClassVirtualSealedEvent
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