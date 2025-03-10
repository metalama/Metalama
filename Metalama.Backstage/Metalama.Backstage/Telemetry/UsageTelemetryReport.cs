// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Telemetry.Metrics;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Metalama.Backstage.Telemetry
{
    internal sealed class UsageTelemetryReport : TelemetryReport
    {
        public override string Kind => "Usage";

        internal UsageTelemetryReport( IServiceProvider serviceProvider, string eventKind )
        {
            var time = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();

            var applicationInfo = serviceProvider.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication;
            var reportedComponent = applicationInfo.GetLatestComponentMadeByPostSharp();

            var loggerFactory = serviceProvider.GetLoggerFactory();

            this.Metrics.Add( new StringMetric( "MetricsEventKind", eventKind ) );

            this.Metrics.Add( new Int32Metric( "Processor.Count", Environment.ProcessorCount ) );
            this.Metrics.Add( new StringMetric( "Processor.Architecture", RuntimeInformation.ProcessArchitecture.ToString() ) );

            this.Metrics.Add( new StringMetric( "OS.Platform", RuntimeInformation.OSDescription ) );

            this.Metrics.Add( new StringMetric( "Net.Architecture", RuntimeInformation.ProcessArchitecture.ToString() ) );
            this.Metrics.Add( new StringMetric( "Net.Version", Environment.Version.ToString() ) );

            this.Metrics.Add( new StringMetric( "Application.Name", reportedComponent.Name ) );
            this.Metrics.Add( new StringMetric( "Application.Version", reportedComponent.PackageVersion ) );
            this.Metrics.Add( new BoolMetric( "Application.IsUnattended", applicationInfo.IsUnattendedProcess( loggerFactory ) ) );
            this.Metrics.Add( new StringMetric( "Application.ProcessName", Process.GetCurrentProcess().ProcessName ) );
            this.Metrics.Add( new StringMetric( "Application.ProcessKind", applicationInfo.ProcessKind.ToString() ) );
            this.Metrics.Add( new StringMetric( "Application.EntryAssembly", Path.GetFileName( Assembly.GetEntryAssembly()?.Location ) ) );

            this.Metrics.Add( new DateTimeMetric( "Time", time.UtcNow ) );
        }
    }
}