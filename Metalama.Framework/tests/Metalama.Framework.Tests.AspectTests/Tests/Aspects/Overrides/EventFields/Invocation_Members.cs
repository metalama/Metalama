using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_Members;
using Metalama.Framework.Code;

#pragma warning disable CS0067

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.EventFields.Invocation_Members
{
    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var @event in builder.Target.Events)
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

    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public event EventHandler? IntroducedEvent;

        [Introduce]
        public static event EventHandler? IntroducedStaticEvent;

        [Introduce]
        public void Bar()
        {
            meta.This.IntroducedEvent?.Invoke( meta.This, new EventArgs() );
            var a = meta.This.IntroducedEvent?.GetInvocationList();
            var b = meta.This.IntroducedEvent?.BeginInvoke( meta.This, new EventArgs(), new AsyncCallback( Callback ), meta.This );
            var c = meta.This.IntroducedEvent?.Method;
            var d = meta.This.IntroducedEvent?.Target;
            meta.ThisType.IntroducedStaticEvent?.Invoke( meta.This, new EventArgs() );
            var e = meta.ThisType.IntroducedStaticEvent?.GetInvocationList();
            var f = meta.ThisType.IntroducedStaticEvent?.BeginInvoke( null, new EventArgs(), new AsyncCallback( Callback ), null );
            var g = meta.ThisType.IntroducedStaticEvent?.Method;
            var h = meta.ThisType.IntroducedStaticEvent?.Target;
        }

        [Introduce]
        private void Callback( IAsyncResult result ) { }
    }

    // <target>
    [Override]
    [Introduction]
    internal class TargetClass
    {
        public event EventHandler? Event;

        public static event EventHandler? StaticEvent;

        public void Foo()
        {
            Event?.Invoke( this, new EventArgs() );
            _ = Event?.GetInvocationList();
            _ = Event?.BeginInvoke( this, new EventArgs(), x => { }, this );
            _ = Event?.Method;
            _ = Event?.Target;
            StaticEvent?.Invoke( this, new EventArgs() );
            _ = StaticEvent?.GetInvocationList();
            _ = StaticEvent?.BeginInvoke( this, new EventArgs(), x => { }, this );
            _ = StaticEvent?.Method;
            _ = StaticEvent?.Target;
        }
    }
}