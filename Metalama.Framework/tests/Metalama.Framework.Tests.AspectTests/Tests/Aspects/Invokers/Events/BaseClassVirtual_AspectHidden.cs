using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden;
using System.Linq;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(InvokerAfterAspect), typeof(IntroductionAspect), typeof(InvokerBeforeAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.BaseClassVirtual_AspectHidden;

/*
 * Tests invokers targeting a virtual event declared in the base class which is hidden by an aspect-introduced event.
 */

public class InvokerBeforeAspect : EventAspect
{
    public override void BuildAspect( IAspectBuilder<IEvent> builder )
    {
        builder.OverrideAccessors(
            nameof(AddTemplate),
            nameof(RemoveTemplate),
            null,
            new { target = builder.Target.DeclaringType!.BaseType!.Events.OfName( "Event" ).Single() } );
    }

    [Template]
    public void AddTemplate( [CompileTime] IEvent target )
    {
        meta.InsertComment( "Invoke this.Event" );
        target.Add( meta.RunTime( TargetClass.StaticTarget ) );
        meta.InsertComment( "Invoke base.Event" );
        target.With( InvokerOptions.Base ).Add( meta.RunTime( TargetClass.StaticTarget ) );
        meta.InsertComment( "Invoke base.Event" );
        target.With( InvokerOptions.Current ).Add( meta.RunTime( TargetClass.StaticTarget ) );
        meta.InsertComment( "Invoke this.Event" );
        target.With( InvokerOptions.Final ).Add( meta.RunTime( TargetClass.StaticTarget ) );

        meta.Proceed();
    }

    [Template]
    public void RemoveTemplate( [CompileTime] IEvent target )
    {
        meta.InsertComment( "Invoke this.Event" );
        target.Remove( meta.RunTime( TargetClass.StaticTarget ) );
        meta.InsertComment( "Invoke base.Event" );
        target.With( InvokerOptions.Base ).Remove( meta.RunTime( TargetClass.StaticTarget ) );
        meta.InsertComment( "Invoke base.Event" );
        target.With( InvokerOptions.Current ).Remove( meta.RunTime( TargetClass.StaticTarget ) );
        meta.InsertComment( "Invoke this.Event" );
        target.With( InvokerOptions.Final ).Remove( meta.RunTime( TargetClass.StaticTarget ) );

        meta.Proceed();
    }
}

public class IntroductionAspect : TypeAspect
{
    [Introduce( WhenExists = OverrideStrategy.New )]
    public event EventHandler Event
    {
        add
        {
            meta.InsertComment( "Invoke base.Event" );
            meta.Target.Event.Add( meta.RunTime( TargetClass.StaticTarget ) );
            meta.InsertComment( "Invoke base.Event" );
            meta.Target.Event.With( InvokerOptions.Base ).Add( meta.RunTime( TargetClass.StaticTarget ) );
            meta.InsertComment( "Invoke this.Event" );
            meta.Target.Event.With( InvokerOptions.Current ).Add( meta.RunTime( TargetClass.StaticTarget ) );
            meta.InsertComment( "Invoke this.Event" );
            meta.Target.Event.With( InvokerOptions.Final ).Add( meta.RunTime( TargetClass.StaticTarget ) );
            meta.InsertComment( "Invoke base.Event" );
            meta.Proceed();
        }

        remove
        {
            meta.InsertComment( "Invoke base.Event" );
            meta.Target.Event.Remove( meta.RunTime( TargetClass.StaticTarget ) );
            meta.InsertComment( "Invoke base.Event" );
            meta.Target.Event.With( InvokerOptions.Base ).Remove( meta.RunTime( TargetClass.StaticTarget ) );
            meta.InsertComment( "Invoke this.Event" );
            meta.Target.Event.With( InvokerOptions.Current ).Remove( meta.RunTime( TargetClass.StaticTarget ) );
            meta.InsertComment( "Invoke this.Event" );
            meta.Target.Event.With( InvokerOptions.Final ).Remove( meta.RunTime( TargetClass.StaticTarget ) );
            meta.InsertComment( "Invoke base.Event" );
            meta.Proceed();
        }
    }
}

public class InvokerAfterAspect : EventAspect
{
    public override void BuildAspect( IAspectBuilder<IEvent> builder )
    {
        builder.OverrideAccessors(
            nameof(AddTemplate),
            nameof(RemoveTemplate),
            null,
            new { target = builder.Target.DeclaringType!.BaseType!.Events.OfName( "Event" ).Single() } );
    }

    [Template]
    public void AddTemplate( [CompileTime] IEvent target )
    {
        meta.InsertComment( "Invoke this.Event" );
        target.Add( meta.RunTime( TargetClass.StaticTarget ) );
        meta.InsertComment( "Invoke this.Event" );
        target.With( InvokerOptions.Base ).Add( meta.RunTime( TargetClass.StaticTarget ) );
        meta.InsertComment( "Invoke this.Event" );
        target.With( InvokerOptions.Current ).Add( meta.RunTime( TargetClass.StaticTarget ) );
        meta.InsertComment( "Invoke this.Event" );
        target.With( InvokerOptions.Final ).Add( meta.RunTime( TargetClass.StaticTarget ) );

        meta.Proceed();
    }

    [Template]
    public void RemoveTemplate( [CompileTime] IEvent target )
    {
        meta.InsertComment( "Invoke this.Event" );
        target.Remove( meta.RunTime( TargetClass.StaticTarget ) );
        meta.InsertComment( "Invoke this.Event" );
        target.With( InvokerOptions.Base ).Remove( meta.RunTime( TargetClass.StaticTarget ) );
        meta.InsertComment( "Invoke this.Event" );
        target.With( InvokerOptions.Current ).Remove( meta.RunTime( TargetClass.StaticTarget ) );
        meta.InsertComment( "Invoke this.Event" );
        target.With( InvokerOptions.Final ).Remove( meta.RunTime( TargetClass.StaticTarget ) );

        meta.Proceed();
    }
}

public class BaseClass
{
    public virtual event EventHandler Event
    {
        add { }
        remove { }
    }
}

// <target>
[IntroductionAspect]
public class TargetClass : BaseClass
{
    [InvokerBeforeAspect]
    public event EventHandler InvokerBefore
    {
        add { }
        remove { }
    }

    [InvokerAfterAspect]
    public event EventHandler InvokerAfter
    {
        add { }
        remove { }
    }

    public static void StaticTarget( object? sender, EventArgs args ) { }
}