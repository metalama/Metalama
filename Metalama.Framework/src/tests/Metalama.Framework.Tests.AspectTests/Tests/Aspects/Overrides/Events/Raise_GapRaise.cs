// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable IDE0052

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_GapRaise;

public class OverrideAttribute : EventAspect
{
    public override void BuildAspect( IAspectBuilder<IEvent> builder )
    {
        builder.OverrideAccessors( nameof( AddEventTemplate ), nameof( RemoveEventTemplate ), null, args: new { ordinal = 0 } );
        builder.OverrideAccessors( nameof( AddEventTemplate ), nameof( RemoveEventTemplate ), nameof( InvokeEventTemplate ), args: new { ordinal = 1 } );
    }

    [Template]
    public void AddEventTemplate( [CompileTime] int ordinal )
    {
        Console.WriteLine( $"Add {ordinal}" );
        meta.Proceed();
    }

    [Template]
    public void RemoveEventTemplate( [CompileTime] int ordinal )
    {
        Console.WriteLine( $"Remove {ordinal}" );
        meta.Proceed();
    }

    [Template]
    public void InvokeEventTemplate( [CompileTime] int ordinal )
    {
        Console.WriteLine( $"Invoke {ordinal}" );
        meta.Proceed();
    }
}

// <target>
internal class TargetClass 
{
    private EventHandler? _handler;

    [Override]
    public event EventHandler Event
    {
        add
        {
            this._handler = value;
        }

        remove
        {
            this._handler = null;
        }
    }
}