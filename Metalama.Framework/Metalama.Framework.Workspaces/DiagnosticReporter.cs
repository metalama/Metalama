// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Workspaces;

/// <summary>
/// A utility class that makes it easy to report diagnostics from object queries in different environments.
/// The default implementation writes messages to the console using the <see cref="ReportToConsole"/> method.
/// The action can be changed by setting the <see cref="ReportAction"/> property.
/// </summary>
[PublicAPI]
public static class DiagnosticReporter
{
    public static int ReportedWarnings { get; private set; }

    public static int ReportedErrors { get; private set; }

    public static void ClearCounters()
    {
        ReportedWarnings = ReportedErrors = 0;
    }

    public static Action<IReadOnlyList<IIntrospectionDiagnostic>>? ReportAction { get; set; } = ReportToConsole;

    public static void ReportToConsole( IReadOnlyList<IIntrospectionDiagnostic> diagnostics )
    {
        foreach ( var d in diagnostics )
        {
            var colorBefore = Console.ForegroundColor;

            switch ( d.Severity )
            {
                case Severity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine( d.FormatAsBuildDiagnostic() );

                    break;

                case Severity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine( d.FormatAsBuildDiagnostic() );

                    break;

                case Severity.Info:
                    Console.WriteLine( d.FormatAsBuildDiagnostic() );

                    break;
            }

            Console.ForegroundColor = colorBefore;
        }
    }

    public static IEnumerable<IIntrospectionDiagnostic> Report(
        this IEnumerable<IIntrospectionReference> references,
        Severity severity,
        string id,
        string message )
        => references
            .SelectMany( r => r.Details )
            .Select( r => new DiagnosticTarget( r.Source.GetDiagnosticLocation(), r.Reference.OriginDeclaration, r ) )
            .GroupBy( r => r.Location?.GetLineSpan().StartLinePosition.Line ) // Report a single warning per line.
            .Select(
                g =>
                {
                    var items = g.ToReadOnlyList();

                    return new DiagnosticTarget( items[0].Location, items[0].Declaration, items.Select( i => i.Details ).ToArray() );
                } )
            .Report( severity, id, message );

    public static IEnumerable<IIntrospectionDiagnostic> Report( this IEnumerable<IDeclaration> declarations, Severity severity, string id, string message )
        => declarations
            .Select( x => new DiagnosticTarget( x.GetDiagnosticLocation(), x, null ) )
            .Report( severity, id, message );

    private sealed record DiagnosticTarget( Location? Location, IDeclaration Declaration, object? Details );

    private static IEnumerable<IIntrospectionDiagnostic> Report( this IEnumerable<DiagnosticTarget> targets, Severity severity, string id, string message )
    {
        var diagnostics = new List<IIntrospectionDiagnostic>();

        foreach ( var location in targets )
        {
            var diagnostic = new UserDiagnostic(
                severity,
                id,
                message,
                location.Location.SourceTree?.FilePath,
                location.Location.GetLineSpan().StartLinePosition.Line + 1,
                location.Declaration,
                location.Details );

            diagnostics.Add( diagnostic );
        }

        return diagnostics.Report();
    }

    private static void IncrementCounters( Severity severity )
    {
        switch ( severity )
        {
            case Severity.Warning:
                ReportedWarnings++;

                break;

            case Severity.Error:
                ReportedErrors++;

                break;
        }
    }

    public static IEnumerable<IIntrospectionDiagnostic> Report( this IEnumerable<IIntrospectionDiagnostic> diagnostics )
    {
        var copy = new List<IIntrospectionDiagnostic>();

        foreach ( var diagnostic in diagnostics )
        {
            IncrementCounters( diagnostic.Severity );
            copy.Add( diagnostic );
        }

        ReportAction?.Invoke( copy );

        return copy;
    }
}