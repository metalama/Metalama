// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.RaiseTargetClass_AspectOverride;
using System;
using System.Linq;

[assembly:AspectOrder(AspectOrderDirection.CompileTime, typeof( InvokerAspectBefore ), typeof( InvokerAspectAfter ) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.RaiseTargetClass_AspectOverride;

/*
 * Tests invokers targeting a event declared in the target class and with overridden raise.
 */

public class InvokerAspectBefore : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override(
            nameof( Template ),
            new { target = builder.Target.DeclaringType!.Events.OfName( "Event" ).Single() } );
    }

    [Template]
    public void Template( [CompileTime] IEvent target )
    {
        meta.InsertComment( "TODO: Invoke this._event" );
        //target.Raise( null, EventArgs.Empty );
        meta.InsertComment( "TODO: Invoke this._event" );
        //target.With( InvokerOptions.Base ).Raise( null, EventArgs.Empty );
        meta.InsertComment( "TODO: Invoke this._event" );
        //target.With( InvokerOptions.Current ).Raise( null, EventArgs.Empty );
        meta.InsertComment( "Invoke this._eventBroker.Invoke" );
        target.With( InvokerOptions.Final ).Raise( null, EventArgs.Empty );

        meta.Proceed();
    }
}
public class InvokerAspectAfter : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override(
            nameof( Template ),
            new { target = builder.Target.DeclaringType!.Events.OfName( "Event" ).Single() } );
    }

    [Template]
    public void Template( [CompileTime] IEvent target )
    {
        meta.InsertComment( "Invoke this._eventBroker.Invoke" );
        target.Raise( null, EventArgs.Empty );
        meta.InsertComment( "Invoke this._eventBroker.Invoke" );
        target.With( InvokerOptions.Base ).Raise( null, EventArgs.Empty );
        meta.InsertComment( "Invoke this._eventBroker.Invoke" );
        target.With( InvokerOptions.Current ).Raise( null, EventArgs.Empty );
        meta.InsertComment( "Invoke this._eventBroker.Invoke" );
        target.With( InvokerOptions.Final ).Raise( null, EventArgs.Empty );

        meta.Proceed();
    }
}

public class OverrideAspect : EventAspect
{
    public override void BuildAspect( IAspectBuilder<IEvent> builder )
    {
        builder.OverrideAccessors( 
            null, 
            null, 
            nameof( RaiseTemplate ),
            new { target = builder.Target.DeclaringType!.Events.OfName( "Event" ).Single() } );
    }

    [Template]
    public void RaiseTemplate( [CompileTime] IEvent target )
    {
        meta.InsertComment( "Invoke handler" );
        target.Raise( null, EventArgs.Empty );
        meta.InsertComment( "Invoke handler" );
        target.With( InvokerOptions.Base ).Raise( null, EventArgs.Empty );
        meta.InsertComment( "TODO: Invoke this._eventBroker.Invoke" );
        //target.With( InvokerOptions.Current ).Raise( null, EventArgs.Empty );
        meta.InsertComment( "TODO: Invoke this._eventBroker.Invoke" );
        //target.With( InvokerOptions.Final ).Raise( null, EventArgs.Empty );
        meta.InsertComment( "Invoke handler" );
        meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    [OverrideAspect]
    public event EventHandler Event;

    [InvokerAspectBefore]
    public void BeforeOverride()
    {
    }

    [InvokerAspectAfter]
    public void AfterOverride()
    {
    }
}