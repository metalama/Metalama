using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;
using Metalama.Framework.Code;
using System.Linq;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_ContainingExpressionBodies
{
    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var @event in builder.Target.Events.Where( e => e.Name is "Event" or "StaticEvent" ))
            {
                builder.With( @event ).OverrideAccessors( nameof(OverrideAdd), nameof(OverrideRemove) );
            }
        }

        [Template]
        public void OverrideAdd( dynamic value )
        {
            Console.WriteLine( "This is the add template." );
            meta.Proceed();
        }

        [Template]
        public void OverrideRemove( dynamic value )
        {
            Console.WriteLine( "This is the remove template." );
            meta.Proceed();
        }
    }

    // <target>
    [Override]
    internal class TargetClass
    {
        public event EventHandler? Event;

        public static event EventHandler? StaticEvent;

        static TargetClass() => StaticEvent?.Invoke( null, new EventArgs() );

        public TargetClass() => Event?.Invoke( this, new EventArgs() );

        ~TargetClass() => Event?.Invoke( this, new EventArgs() );

        public void Foo() => Event?.Invoke( this, new EventArgs() );

        public static void Bar() => StaticEvent?.Invoke( null, new EventArgs() );

        public int Baz
        {
            init => Event?.Invoke( this, new EventArgs() );
        }

        public event EventHandler? Quz
        {
            add => Event?.Invoke( this, new EventArgs() );
            remove => Event?.Invoke( this, new EventArgs() );
        }

        public int this[ int index ]
        {
            set => Event?.Invoke( this, new EventArgs() );
        }
    }
}