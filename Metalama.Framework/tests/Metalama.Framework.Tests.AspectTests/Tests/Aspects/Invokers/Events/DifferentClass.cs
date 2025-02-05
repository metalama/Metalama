using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System.Linq;

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.DifferentClass;

/*
 * Tests default and final invokers targeting a event declared in a different class.
 */

public class InvokerAspect : EventAspect
{
    public override void BuildAspect( IAspectBuilder<IEvent> builder )
    {
        builder.OverrideAccessors(
            nameof(AddTemplate),
            nameof(RemoveTemplate),
            null,
            new { target = ( (INamedType)builder.Target.DeclaringType.Fields.Single().Type ).Events.OfName( "Event" ).Single() } );
    }

    [Template]
    public void AddTemplate( [CompileTime] IEvent target )
    {
        meta.InsertComment( "Invoke instance.Event" );
        target.With( (IExpression?)meta.Target.Event.DeclaringType.Fields.Single().Value ).Add( meta.RunTime( TargetClass.StaticTarget ) );
        meta.InsertComment( "Invoke instance.Event" );

        target.With( (IExpression?)meta.Target.Event.DeclaringType.Fields.Single().Value, InvokerOptions.Final )
            .Add( meta.RunTime( TargetClass.StaticTarget ) );

        meta.Proceed();
    }

    [Template]
    public void RemoveTemplate( [CompileTime] IEvent target )
    {
        meta.InsertComment( "Invoke instance.Event" );
        target.With( (IExpression?)meta.Target.Event.DeclaringType.Fields.Single().Value ).Remove( meta.RunTime( TargetClass.StaticTarget ) );
        meta.InsertComment( "Invoke instance.Event" );

        target.With( (IExpression?)meta.Target.Event.DeclaringType.Fields.Single().Value, InvokerOptions.Final )
            .Remove( meta.RunTime( TargetClass.StaticTarget ) );

        meta.Proceed();
    }
}

public class DifferentClass
{
    public event EventHandler Event
    {
        add { }
        remove { }
    }
}

// <target>
public class TargetClass
{
    private DifferentClass? instance;

    [InvokerAspect]
    public event EventHandler Invoker
    {
        add { }
        remove { }
    }

    public static void StaticTarget( object? sender, EventArgs args ) { }
}