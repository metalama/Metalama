// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable CS0067

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Events.RaiseMethod_Nullability;

/// <summary>
/// Tests that introduced field-like events have a non-null RaiseMethod and CanRaise=true,
/// while introduced non-field-like events (with explicit add/remove accessors) have a null
/// RaiseMethod and CanRaise=false.
/// </summary>
public class IntroductionAttribute : TypeAspect
{
    private static readonly DiagnosticDefinition<(string EventName, bool HasRaiseMethod, bool CanRaise)> _diagnostic =
        new( "MY001", Severity.Warning, "Event '{0}': HasRaiseMethod={1}, CanRaise={2}" );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce a field-like event.
        var fieldLikeResult = builder.IntroduceEvent( nameof( FieldLikeEvent ), buildEvent: e => e.Accessibility = Accessibility.Public );

        // Introduce a non-field-like event (with explicit accessors from template).
        var accessorResult = builder.IntroduceEvent( nameof( AccessorEvent ), buildEvent: e => e.Accessibility = Accessibility.Public );

        // Introduce a non-field-like event via individual accessor templates.
        var programmaticResult = builder.IntroduceEvent(
            "ProgrammaticAccessorEvent",
            nameof( AddEventTemplate ),
            nameof( RemoveEventTemplate ),
            buildEvent: e => e.Accessibility = Accessibility.Public );

        // Report diagnostics about each event.
        ReportEventInfo( builder, fieldLikeResult.Declaration );
        ReportEventInfo( builder, accessorResult.Declaration );
        ReportEventInfo( builder, programmaticResult.Declaration );
    }

    private void ReportEventInfo( IAspectBuilder builder, IEvent @event )
    {
        builder.Diagnostics.Report(
            _diagnostic.WithArguments( (@event.Name, @event.RaiseMethod != null, @event.CanRaise) ) );
    }

    [Template]
    public event EventHandler? FieldLikeEvent;

    [Template]
    public event EventHandler? AccessorEvent
    {
        add
        {
            Console.WriteLine( "Add" );
        }

        remove
        {
            Console.WriteLine( "Remove" );
        }
    }

    [Template]
    public void AddEventTemplate( EventHandler value )
    {
        Console.WriteLine( "Add" );
        meta.Proceed();
    }

    [Template]
    public void RemoveEventTemplate( EventHandler value )
    {
        Console.WriteLine( "Remove" );
        meta.Proceed();
    }
}

// <target>
[Introduction]
internal class TargetClass { }
