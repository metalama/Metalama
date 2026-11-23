// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Workspaces;
using SharpCrafters.Backstage.Diagnostics;
using SharpCrafters.Backstage.Extensibility;
using SharpCrafters.Backstage.Telemetry;
using SharpCrafters.Backstage.Telemetry.Metrics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Metalama.Introspection.Navigation.Telemetry;

/// <summary>
/// Reports the usage of Metalama.Mcp and Metalama.LinqPad, and their unexpected exceptions, through the telemetry of
/// Backstage.
/// </summary>
/// <remarks>
/// <para>
/// Every report goes through an <see cref="ITelemetryContext"/>, so that the consent of the user, the opt-out of the
/// repository (<c>metalama.json</c>) and the process-level gates apply. The reports about a workspace use the context that
/// the workspace opened when it was loaded (<see cref="IWorkspaceHostInfo.TelemetryContext"/>). Only an exception that occurs outside
/// of a workspace, for instance while a workspace is loaded, opens a context for a directory. Nothing is sent for a directory
/// that is not in a git repository.
/// </para>
/// <para>
/// A usage report is produced at most once per workspace and per day. It contains counts and the names of
/// frameworks and clients only. It never contains a path, a workspace name, a project name, a declaration or the text
/// of a query.
/// </para>
/// <para>
/// No method of this class throws. Telemetry must never cause a load or a query to fail.
/// </para>
/// </remarks>
internal static class IntrospectionTelemetry
{
    /// <summary>
    /// The maximal length of a string value of a metric.
    /// </summary>
    internal const int MaxValueLength = 64;

    /// <summary>
    /// Reports that a workspace was loaded. The report is produced at most once per workspace and per day.
    /// </summary>
    /// <param name="host">The component that loaded the workspace.</param>
    /// <param name="workspaceKey">A key that identifies the workspace on this device. It is used to claim the daily report and is never sent.</param>
    /// <param name="paths">The paths of the projects or solutions of the workspace. Only their extensions are sent.</param>
    /// <param name="workspace">The workspace.</param>
    /// <param name="loadDuration">The time the load took.</param>
    /// <param name="clientName">The name of the client: the MCP client, or LINQPad.</param>
    /// <param name="clientVersion">The version of the client.</param>
    public static void ReportWorkspaceLoaded(
        IntrospectionHost host,
        string workspaceKey,
        IEnumerable<string> paths,
        Workspace workspace,
        TimeSpan loadDuration,
        string? clientName,
        string? clientVersion )
    {
        var pathList = paths.ToList();

        try
        {
            // The context of the workspace decides whether the usage session is metered.
            var context = ((IWorkspaceHostInfo) workspace).TelemetryContext;

            if ( context == null )
            {
                return;
            }

            using var session = context.StartUsageSession( GetSessionKind( host ), $"<Metalama.{host}>:{workspaceKey}" );

            if ( session.ShouldCollectMetrics )
            {
                var diagnostics = workspace.WorkspaceDiagnostics;

                var usage = new WorkspaceUsage(
                    host,
                    pathList.Select( Path.GetExtension ).OfType<string>(),
                    workspace.Projects.Select( p => p.Name ).Distinct( StringComparer.OrdinalIgnoreCase ).Count(),
                    workspace.Projects.Where( p => p.IsMetalamaEnabled ).Select( p => p.Name ).Distinct( StringComparer.OrdinalIgnoreCase ).Count(),
                    workspace.Projects.Select( p => p.TargetFramework ).OfType<string>(),
                    loadDuration,
                    diagnostics.Count( d => d.Severity == Framework.Diagnostics.Severity.Error ),
                    diagnostics.Count( d => d.Severity == Framework.Diagnostics.Severity.Warning ),
                    clientName,
                    clientVersion );

                AddMetrics( session.Metrics, usage );
            }
        }
        catch ( Exception e ) when ( e is not OutOfMemoryException )
        {
            ReportWorkspaceException( e, workspace );
        }
    }

    /// <summary>
    /// Reports an unexpected exception that occurred while a workspace was used, through the telemetry context of the
    /// workspace, unless the exception is attributable to the user or to the environment of the user.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="workspace">The workspace whose use failed.</param>
    public static void ReportWorkspaceException( Exception exception, Workspace workspace )
    {
        try
        {
            if ( ((IWorkspaceHostInfo) workspace).TelemetryContext is { } context && Classify( exception ) is { } classifiedException )
            {
                context.ReportException( classifiedException );
            }
        }
        catch ( Exception e ) when ( e is not OutOfMemoryException )
        {
            // Reporting must never fail the request that failed. The exception is written to the standard error stream,
            // which the user can read in the log of the host.
            Console.Error.WriteLine( $"Cannot report an exception to the telemetry: {e.Message}" );
        }
    }

    /// <summary>
    /// Reports an unexpected exception through the exception telemetry, unless the exception is attributable to the user
    /// or to the environment of the user.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="directory">
    /// The directory that determines the telemetry policy: the directory of the workspace, or <see langword="null"/> to use
    /// the working directory of the process.
    /// </param>
    /// <remarks>
    /// The exceptions that are not reported are the ones that <see cref="IsUserAttributable"/> recognizes, and the ones
    /// that <see cref="ExceptionClassifier"/> does not classify as errors, such as input and output errors.
    /// </remarks>
    public static void ReportException( Exception exception, string? directory )
    {
        try
        {
            if ( Classify( exception ) is not { } classifiedException )
            {
                return;
            }

            var telemetryService = BackstageServiceFactoryInitializer.ServiceProvider.GetBackstageService<ITelemetryService>();

            if ( telemetryService == null )
            {
                return;
            }

            var policy = string.IsNullOrEmpty( directory ) ? telemetryService.GetToolingPolicy() : telemetryService.GetPolicy( directory );

            telemetryService.OpenContext( policy ).ReportException( classifiedException );
        }
        catch ( Exception e ) when ( e is not OutOfMemoryException )
        {
            // Reporting must never fail the request that failed. The exception is written to the standard error stream,
            // which the user can read in the log of the host.
            Console.Error.WriteLine( $"Cannot report an exception to the telemetry: {e.Message}" );
        }
    }

    /// <summary>
    /// Classifies an exception, and returns <see langword="null"/> when the exception must not be reported: when it is
    /// attributable to the user or to the environment of the user, or when <see cref="ExceptionClassifier"/> does not
    /// classify it as an error, such as an input and output error.
    /// </summary>
    private static ClassifiedException? Classify( Exception exception )
    {
        if ( IsUserAttributable( exception ) )
        {
            return null;
        }

        var classifiedException = ExceptionClassifier.Classify( exception );

        return classifiedException.IsError ? classifiedException : null;
    }

    /// <summary>
    /// Determines whether an exception is attributable to the user or to the environment of the user rather than to a
    /// defect of the product.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>
    /// <see langword="true"/> for a <see cref="DiagnosticException"/>, which reports a problem of the user's code or
    /// build, and for a <see cref="MSBuildInitializationException"/>, which reports that no compatible .NET SDK is
    /// installed, including when either is wrapped in another exception.
    /// </returns>
    internal static bool IsUserAttributable( Exception exception )
    {
        if ( DiagnosticException.TryFind( exception ) != null )
        {
            return true;
        }

        for ( var e = exception; e != null; e = e.InnerException )
        {
            if ( e is MSBuildInitializationException )
            {
                return true;
            }

            if ( e is AggregateException aggregate && aggregate.InnerExceptions.Any( IsUserAttributable ) )
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Adds the metrics of a workspace load to a usage report.
    /// </summary>
    /// <param name="metrics">The metrics of the report. It must not be read-only.</param>
    /// <param name="usage">The facts about the load.</param>
    internal static void AddMetrics( MetricCollection metrics, WorkspaceUsage usage )
    {
        metrics.Add( new StringMetric( "Introspection.Host", usage.Host.ToString() ) );
        metrics.Add( new StringMetric( "Introspection.Client.Name", Sanitize( usage.ClientName ) ) );
        metrics.Add( new StringMetric( "Introspection.Client.Version", Sanitize( usage.ClientVersion ) ) );

        var kinds = new SetMetric( "Introspection.Workspace.Kinds" );

        foreach ( var kind in usage.Kinds )
        {
            if ( Sanitize( kind.ToLowerInvariant() ) is { Length: > 0 } sanitizedKind )
            {
                kinds.SetValue( sanitizedKind );
            }
        }

        metrics.Add( kinds );

        metrics.Add( new Int32Metric( "Introspection.Workspace.ProjectCount", usage.ProjectCount ) );
        metrics.Add( new Int32Metric( "Introspection.Workspace.MetalamaProjectCount", usage.MetalamaProjectCount ) );

        var targetFrameworks = new SetMetric( "Introspection.Workspace.TargetFrameworks" );

        foreach ( var targetFramework in usage.TargetFrameworks )
        {
            if ( Sanitize( targetFramework ) is { Length: > 0 } sanitizedTargetFramework )
            {
                targetFrameworks.SetValue( sanitizedTargetFramework );
            }
        }

        metrics.Add( targetFrameworks );

        metrics.Add( new Int32Metric( "Introspection.Workspace.LoadDurationMilliseconds", (int) Math.Min( usage.LoadDuration.TotalMilliseconds, int.MaxValue ) ) );
        metrics.Add( new Int32Metric( "Introspection.Workspace.ErrorCount", usage.ErrorCount ) );
        metrics.Add( new Int32Metric( "Introspection.Workspace.WarningCount", usage.WarningCount ) );
    }

    /// <summary>
    /// Makes a string safe to be written in a usage report.
    /// </summary>
    /// <param name="value">The string, or <see langword="null"/>.</param>
    /// <returns>
    /// The string without the characters that separate the metrics of a report (<c>;</c> and <c>=</c>) or the values of a
    /// set (<c>,</c>), without control characters, and truncated to <see cref="MaxValueLength"/> characters. An empty
    /// string when <paramref name="value"/> is <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// The report is written as a single line of <c>Name=value</c> pairs separated by <c>;</c>, and the values are not
    /// escaped, so these characters would corrupt the report.
    /// </remarks>
    internal static string Sanitize( string? value )
    {
        if ( string.IsNullOrEmpty( value ) )
        {
            return "";
        }

        var builder = new StringBuilder( Math.Min( value.Length, MaxValueLength ) );

        foreach ( var c in value )
        {
            if ( builder.Length == MaxValueLength )
            {
                break;
            }

            if ( c is ';' or '=' or ',' || char.IsControl( c ) )
            {
                continue;
            }

            builder.Append( c );
        }

        return builder.ToString().Trim();
    }

    private static string GetSessionKind( IntrospectionHost host ) => $"{host}Usage";
}
