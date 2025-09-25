// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TESTOPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Samples.SafeEvent;

internal class SafeEventAttribute : OverrideEventAspect
{
    public override void OverrideAdd( dynamic value )
    {
        meta.Proceed();
    }

    public override void OverrideRemove( dynamic value )
    {
        meta.Proceed();
    }

    public override dynamic? OverrideInvoke( dynamic? handler )
    {
        try
        {
            return meta.Proceed();
        }
        catch ( Exception e )
        {
            Console.WriteLine( e );

            // First time shame on you, second time shame on me.
            meta.Target.Event.Remove( handler );
            throw;
        }
    }
}

// <target>
internal class TargetCode
{
    private List<EventHandler> _delegates = new List<EventHandler>();

    [SafeEvent]
    public event EventHandler EventField;

    [SafeEvent]
    public event EventHandler Event
    {
        add
        {
            this._delegates.Add( value );
        }

        remove
        {
            this._delegates.Remove( value );
        }
    }

    public void OnEventField()
    {
        this.EventField.Invoke( this, EventArgs.Empty );
    }

    public void OnEvent()
    {
        foreach(var @delegate in this._delegates)
        {
            @delegate.Invoke( this, EventArgs.Empty );
        }
    }
}
