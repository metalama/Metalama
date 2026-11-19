// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage;
using Metalama.Extensions.Metrics;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Introspection;
using Metalama.Framework.Workspaces;
using SharpCrafters.Backstage.Extensibility;
using SharpCrafters.Backstage.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Mcp.Worker;

/// <summary>
/// Initializes Metalama in the worker, as <c>DriverInitialization</c> does in Metalama.LinqPad, but headless.
/// </summary>
internal static class WorkerInitialization
{
    private static readonly List<IIntrospectionDiagnostic> _reportedDiagnostics = [];

    public static void Initialize()
    {
        if ( !string.IsNullOrEmpty( Environment.GetEnvironmentVariable( "METALAMA_MCP_DEBUG" ) ) )
        {
            Debugger.Launch();
        }

        if ( !BackstageServiceFactoryInitializer.IsInitialized )
        {
            // Don't enforce licensing in workspaces. The user interface services are needed for telemetry, as they are in
            // the compiler: they show the notification that lets the user review an exception report, and they open the
            // welcome page once, when usage telemetry is activated on the device. Without them, a captured exception
            // report is never shown and never sent (#2386). These services start a separate process to show a notification
            // or open a page, so the worker itself still has no user interface. The detection of license and news
            // notifications stays disabled.
            BackstageServiceFactoryInitializer.Initialize(
                new BackstageInitializationOptions( new McpApplicationInfo(), MetalamaProduct.Instance )
                {
                    AddSupportServices = true, AddUserInterface = true, OpenWelcomePage = true, DetectToastNotifications = false
                } );
        }

        // Register standard metric providers with the default workspace collection.
        WorkspaceCollection.Default.ServiceBuilder.AddMetrics();

        DiagnosticReporter.ReportAction = diagnostics =>
        {
            lock ( _reportedDiagnostics )
            {
                _reportedDiagnostics.AddRange( diagnostics );
            }
        };
    }

    /// <summary>
    /// Stops the queue of background tasks of Backstage and waits for the tasks that it already holds, such as the upload
    /// of a telemetry report, for at most a given time.
    /// </summary>
    /// <param name="timeout">The maximal time to wait.</param>
    /// <remarks>
    /// A process that exits with <see cref="Environment.Exit"/> kills a background task that has not started yet. This
    /// method never throws.
    /// </remarks>
    public static async Task CompleteBackgroundTasksAsync( TimeSpan timeout )
    {
        try
        {
            if ( BackstageServiceFactoryInitializer.IsInitialized
                 && BackstageServiceFactoryInitializer.ServiceProvider.GetBackstageService<BackstageBackgroundTasksService>() is { } backgroundTasks )
            {
                await backgroundTasks.CompleteAsync().WaitAsync( timeout, CancellationToken.None );
            }
        }
        catch ( Exception e ) when ( e is not OutOfMemoryException )
        {
            // A timeout, or a failure of a task, must not prevent the worker from exiting.
            await Console.Error.WriteLineAsync( $"The background tasks did not complete: {e.Message}" );
        }
    }

    /// <summary>
    /// Gets and clears the diagnostics that queries reported through <see cref="DiagnosticReporter"/>, and resets its counters.
    /// </summary>
    public static IReadOnlyList<IIntrospectionDiagnostic> TakeReportedDiagnostics()
    {
        lock ( _reportedDiagnostics )
        {
            var diagnostics = _reportedDiagnostics.ToArray();
            _reportedDiagnostics.Clear();
            DiagnosticReporter.ClearCounters();

            return diagnostics;
        }
    }
}
