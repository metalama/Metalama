// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TESTOPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.RaiseTargetClass;

/*
 * Tests invokers targeting a event declared in the target class.
 */

public class InvokerAspect : MethodAspect
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
        meta.InsertComment( "Raise this.Event" );
        target.Raise( null, EventArgs.Empty );
        meta.InsertComment( "Raise this.Event" );
        target.With( InvokerOptions.Base ).Raise( null, EventArgs.Empty );
        meta.InsertComment( "Raise this.Event" );
        target.With( InvokerOptions.Current ).Raise( null, EventArgs.Empty );
        meta.InsertComment( "Raise this.Event" );
        target.With( InvokerOptions.Final ).Raise( null, EventArgs.Empty );

        meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    public event EventHandler Event;

    [InvokerAspect]
    public void Foo()
    {

    }
}