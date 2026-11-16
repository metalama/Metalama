// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage;
using Metalama.Extensions.Metrics;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Introspection;
using Metalama.Framework.Workspaces;
using SharpCrafters.Backstage.Application;
using SharpCrafters.Backstage.Extensibility;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
            // Don't enforce licensing in workspaces. The worker has no user interface, so nothing may open a page or show a
            // notification.
            BackstageServiceFactoryInitializer.Initialize(
                new BackstageInitializationOptions( new McpApplicationInfo(), MetalamaProduct.Instance )
                {
                    AddSupportServices = true, OpenWelcomePage = false, DetectToastNotifications = false
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

    private sealed class McpApplicationInfo : ApplicationInfoBase
    {
        public McpApplicationInfo() : base( typeof(McpApplicationInfo).Assembly, MetalamaProduct.Profile ) { }

        public override string Name => "Metalama.Mcp";
    }
}
