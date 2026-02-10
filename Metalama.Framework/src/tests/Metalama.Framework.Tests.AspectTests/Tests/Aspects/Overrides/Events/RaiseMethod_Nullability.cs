// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable CS0067

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.RaiseMethod_Nullability;

/// <summary>
/// Tests that source code events report correct RaiseMethod/CanRaise:
/// - Field-like events: RaiseMethod is non-null, CanRaise is true.
/// - Accessor-based events: RaiseMethod is null, CanRaise is false.
/// - Abstract events: RaiseMethod is null, CanRaise is false.
/// </summary>
public class CheckRaiseMethodAttribute : EventAspect
{
    private static readonly DiagnosticDefinition<(string EventName, bool HasRaiseMethod, bool CanRaise)> _diagnostic =
        new( "MY001", Severity.Warning, "Event '{0}': HasRaiseMethod={1}, CanRaise={2}" );

    public override void BuildAspect( IAspectBuilder<IEvent> builder )
    {
        var e = builder.Target;

        builder.Diagnostics.Report(
            _diagnostic.WithArguments( (e.Name, e.RaiseMethod != null, e.CanRaise) ) );
    }
}

// <target>
internal class TargetClass
{
    [CheckRaiseMethod]
    public event EventHandler? FieldLikeEvent;

    [CheckRaiseMethod]
    public event EventHandler AccessorEvent
    {
        add { }
        remove { }
    }
}

internal abstract class AbstractTargetClass
{
    [CheckRaiseMethod]
    public abstract event EventHandler AbstractEvent;
}

internal interface ITargetInterface
{
    [CheckRaiseMethod]
    event EventHandler InterfaceEvent;
}
