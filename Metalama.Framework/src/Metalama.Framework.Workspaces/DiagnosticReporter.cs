// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
/// A utility class that makes it easy to report diagnostics from code queries in different environments.
/// The default implementation writes messages to the console using the <see cref="ReportToConsole"/> method.
/// The action can be changed by setting the <see cref="ReportAction"/> property.
/// </summary>
/// <seealso cref="IIntrospectionDiagnostic"/>
/// <seealso cref="DiagnosticExtensions"/>
/// <seealso href="@introspection-api"/>
[PublicAPI]
public static class DiagnosticReporter
{
    /// <summary>
    /// Gets the number of warnings reported since the last call to <see cref="ClearCounters"/>.
    /// </summary>
    public static int ReportedWarnings { get; private set; }

    /// <summary>
    /// Gets the number of errors reported since the last call to <see cref="ClearCounters"/>.
    /// </summary>
    public static int ReportedErrors { get; private set; }

    /// <summary>
    /// Clears the <see cref="ReportedWarnings"/> and <see cref="ReportedErrors"/> counters.
    /// </summary>
    public static void ClearCounters()
    {
        ReportedWarnings = ReportedErrors = 0;
    }

    /// <summary>
    /// Gets or sets the action to execute when diagnostics are reported. The default action is <see cref="ReportToConsole"/>.
    /// </summary>
    public static Action<IReadOnlyList<IIntrospectionDiagnostic>>? ReportAction { get; set; } = ReportToConsole;

    /// <summary>
    /// Reports diagnostics to the console with color-coded output based on severity.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to report.</param>
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

    /// <summary>
    /// Creates and reports diagnostics for a set of introspection references.
    /// </summary>
    /// <param name="references">The references to report diagnostics for.</param>
    /// <param name="severity">The severity of the diagnostics.</param>
    /// <param name="id">The diagnostic identifier.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <returns>An enumerable of diagnostics that were reported.</returns>
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

    /// <summary>
    /// Creates and reports diagnostics for a set of declarations.
    /// </summary>
    /// <param name="declarations">The declarations to report diagnostics for.</param>
    /// <param name="severity">The severity of the diagnostics.</param>
    /// <param name="id">The diagnostic identifier.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <returns>An enumerable of diagnostics that were reported.</returns>
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
                location.Location?.SourceTree?.FilePath,
                location.Location?.GetLineSpan().StartLinePosition.Line + 1,
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

    /// <summary>
    /// Reports a collection of diagnostics using the configured <see cref="ReportAction"/> and updates the diagnostic counters.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to report.</param>
    /// <returns>A copy of the reported diagnostics.</returns>
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