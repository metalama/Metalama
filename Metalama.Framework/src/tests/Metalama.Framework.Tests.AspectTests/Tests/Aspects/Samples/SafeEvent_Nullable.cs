// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
// @LanguageVersion(12.0)
// @ClearIgnoredDiagnostics
#endif

// Issue #1229: EventBroker pattern generates multiple warnings with nullable events

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;

#nullable enable

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Samples.SafeEvent_Nullable;

internal class SafeEventAttribute : OverrideEventAspect
{
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

    // Field-like event with nullable delegate type.
    [SafeEvent]
    public event EventHandler? NullableEventField;

    // Explicitly-implemented event with nullable delegate type.
    [SafeEvent]
    public event EventHandler? NullableEvent
    {
        add
        {
            if ( value != null )
            {
                this._delegates.Add( value );
            }
        }

        remove
        {
            if ( value != null )
            {
                this._delegates.Remove( value );
            }
        }
    }

    public void OnNullableEventField()
    {
        this.NullableEventField?.Invoke( this, EventArgs.Empty );
    }

    public void OnNullableEvent()
    {
        foreach ( var @delegate in this._delegates )
        {
            @delegate.Invoke( this, EventArgs.Empty );
        }
    }
}
