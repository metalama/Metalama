// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using LINQPad;
using Metalama.Backstage;
using Metalama.Extensions.Metrics;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Workspaces;
using Metalama.Introspection.Navigation.Telemetry;
using SharpCrafters.Backstage.Application;
using SharpCrafters.Backstage.Extensibility;
using System;

namespace Metalama.LinqPad;

internal static class DriverInitialization
{
    private static readonly object _sync = new();
    private static bool _isInitialized;

    public static void Initialize()
    {
        // Reset counters every time. 
        DiagnosticReporter.ClearCounters();

        lock ( _sync )
        {
            if ( _isInitialized )
            {
                return;
            }
            else
            {
                _isInitialized = true;
            }

            if ( !BackstageServiceFactoryInitializer.IsInitialized )
            {
                // Don't enforce licensing in workspaces. The user interface services show the notification that lets the
                // user review an exception report, and open the welcome page once, when usage telemetry is activated on the
                // device. Without them, a captured exception report is never shown and never sent (#2386).
                BackstageServiceFactoryInitializer.Initialize(
                    new BackstageInitializationOptions( new LinqPadApplicationInfo(), MetalamaProduct.Instance )
                    {
                        AddSupportServices = true, AddUserInterface = true
                    } );
            }

            // Register standard metric providers with the default workspace collection.
            WorkspaceCollection.Default.ServiceBuilder.AddMetrics();

            // Both drivers load workspaces through the default collection: the workspace driver in its data context, and
            // the scratchpad driver in the queries of the user. Each load is reported in the usage telemetry (#2384).
            WorkspaceCollection.Default.WorkspaceLoaded += OnWorkspaceLoaded;

            DiagnosticReporter.ReportAction = diagnostics => diagnostics.Dump( "Error List" );
        }
    }

    /// <summary>
    /// Reports a workspace load in the usage telemetry.
    /// </summary>
    private static void OnWorkspaceLoaded( Workspace workspace, TimeSpan elapsed )
        => IntrospectionTelemetry.ReportWorkspaceLoaded(
            IntrospectionHost.LinqPad,
            workspace.Key,
            workspace.LoadedPaths,
            workspace,
            elapsed,
            "LINQPad",
            typeof(Util).Assembly.GetName().Version?.ToString() );

    private sealed class LinqPadApplicationInfo : ApplicationInfoBase
    {
        public LinqPadApplicationInfo() : base( typeof(LinqPadApplicationInfo).Assembly, MetalamaProduct.Profile ) { }

        public override string Name => "Metalama.LinqPad";

        /// <inheritdoc />
        /// <remarks>
        /// The driver is counted separately from the compiler, because it is also used on code that does not use Metalama.
        /// </remarks>
        public override string UsageAnalyticsActionName => "linqpad";
    }
}