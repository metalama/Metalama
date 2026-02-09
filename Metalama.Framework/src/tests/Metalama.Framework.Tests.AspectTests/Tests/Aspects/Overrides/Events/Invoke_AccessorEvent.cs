// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TESTOPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
// @LanguageVersion(12.0)
#endif

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

#pragma warning disable IDE0052

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Invoke_AccessorEvent;

/// <summary>
/// Tests that an accessor-based event (with null RaiseMethod) can still have
/// its InvokeHandler semantic overridden. This demonstrates that the
/// GetRaiseMethodForAdvice() internal method provides a PseudoRaiser for template
/// binding even when the public RaiseMethod is null.
/// </summary>
public class OverrideAttribute : EventAspect
{
    private static readonly DiagnosticDefinition<(string EventName, bool HasRaiseMethod, bool CanRaise)> _diagnostic =
        new( "MY001", Severity.Warning, "Event '{0}': HasRaiseMethod={1}, CanRaise={2}" );

    public override void BuildAspect( IAspectBuilder<IEvent> builder )
    {
        // Report the event's RaiseMethod state before overriding.
        var e = builder.Target;

        builder.Diagnostics.Report(
            _diagnostic.WithArguments( (e.Name, e.RaiseMethod != null, e.CanRaise) ) );

        // Override only the invoke handler — works even for accessor-based events.
        builder.OverrideAccessors( invokeTemplate: nameof( InvokeEventTemplate ) );
    }

    [Template]
    public void InvokeEventTemplate()
    {
        Console.WriteLine( "Invoke override" );
        meta.Proceed();
    }
}

// <target>
internal class TargetClass
{
    private EventHandler? _handler;

    [Override]
    public event EventHandler AccessorEvent
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
