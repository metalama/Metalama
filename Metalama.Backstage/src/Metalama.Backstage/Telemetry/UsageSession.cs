// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
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

namespace Metalama.Backstage.Telemetry;

internal sealed class UsageSession : IUsageSession
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _kind;
    private readonly TelemetryReportUploader _telemetryReportUploader;
    private readonly ILogger _logger;
    private readonly MatomoUploader? _matomoUploader;
    private readonly IDateTimeProvider _time;
    private readonly IConfigurationManager _configurationManager;
    private readonly BackstageBackgroundTasksService _backgroundTasksService;

    private bool _isDisposed;

    public bool ShouldCollectMetrics { get; set; }

    public MetricCollection Metrics { get; }

    public UsageSession( IServiceProvider serviceProvider, string kind, bool shouldCollectMetrics )
    {
        this.ShouldCollectMetrics = shouldCollectMetrics;
        this._serviceProvider = serviceProvider;
        this._kind = kind;

        this._telemetryReportUploader = serviceProvider.GetRequiredBackstageService<TelemetryReportUploader>();
        this._logger = serviceProvider.GetLoggerFactory().Telemetry();
        this._matomoUploader = serviceProvider.GetBackstageService<MatomoUploader>();
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
        this._time = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._backgroundTasksService = serviceProvider.GetRequiredBackstageService<BackstageBackgroundTasksService>();

        if ( shouldCollectMetrics )
        {
            this.Metrics = new MetricCollection();
            this.InitializeMetrics( serviceProvider );
        }
        else
        {
            this.Metrics = MetricCollection.EmptyReadOnly;
        }

        if ( this._logger.Trace != null )
        {
            this._logger.Trace.Log( $"Usage session started." );
            this.TraceSample();
        }
    }

    private void InitializeMetrics( IServiceProvider serviceProvider )
    {
        var time = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();

        var applicationInfo = serviceProvider.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication;
        var reportedComponent = applicationInfo.GetLatestComponentMadeByPostSharp();

        var loggerFactory = serviceProvider.GetLoggerFactory();

        this.Metrics.Add( new StringMetric( "MetricsEventKind", this._kind ) );

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

    public void Dispose()
    {
        if ( this._isDisposed )
        {
            return;
        }

        this._isDisposed = true;

        if ( this._logger.Trace != null )
        {
            this._logger.Trace.Log( $"Usage session ended." );
            this.TraceSample();
        }

        this.Metrics.Freeze();

        UsageTelemetryReport? report = null;

        UsageTelemetryReport GetReport() => report ??= report = new UsageTelemetryReport( this._serviceProvider, this.Metrics );

        if ( this.ShouldCollectMetrics )
        {
            this._telemetryReportUploader.Upload( GetReport() );
        }

        if ( this._matomoUploader != null )
        {
            var mustPerformAggregateAudit = this._configurationManager.UpdateIf<TelemetryConfiguration>(
                c => c.LastMatomoPostTime == null || c.LastMatomoPostTime <= this._time.UtcNow.AddDays( -1 ),
                c => c with { LastMatomoPostTime = this._time.UtcNow } );

            if ( mustPerformAggregateAudit )
            {
                this._backgroundTasksService.Enqueue( () => this._matomoUploader.SendUsageAuditAsync( GetReport() ) );
            }
        }
    }

    private void TraceSample()
    {
        if ( this._logger.Trace == null )
        {
            return;
        }

        foreach ( var metric in this.Metrics )
        {
            this._logger.Trace.Log( $"  {metric.Name}: {metric}" );
        }
    }
}