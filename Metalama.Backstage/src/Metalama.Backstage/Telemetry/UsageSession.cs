// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Telemetry;

internal sealed class UsageSession : IUsageSession
{
    private readonly TelemetryReportUploader _telemetryReportUploader;
    private readonly ILogger _logger;

    private UsageTelemetryReport? _usageSample;

    public string Kind { get; }

    public MetricCollection Metrics => this._usageSample?.Metrics ?? throw new InvalidOperationException( "Usage session has ended." );

    public UsageSession( IServiceProvider serviceProvider, string kind )
    {
        this._usageSample = new UsageTelemetryReport( serviceProvider, kind );
        this._telemetryReportUploader = serviceProvider.GetRequiredBackstageService<TelemetryReportUploader>();
        this._logger = serviceProvider.GetLoggerFactory().Telemetry();
        this.Kind = kind;

        if ( this._logger.Trace != null )
        {
            this._logger.Trace.Log( $"Usage session started." );
            this.TraceSample();
        }
    }

    public void Dispose()
    {
        if ( this._usageSample == null )
        {
            return;
        }

        if ( this._logger.Trace != null )
        {
            this._logger.Trace.Log( $"Usage session ended." );
            this.TraceSample();
        }

        this._telemetryReportUploader.Upload( this._usageSample );
        this._usageSample = null;
    }

    private void TraceSample()
    {
        if ( this._logger.Trace == null )
        {
            return;
        }

        foreach ( var metric in this._usageSample?.Metrics ?? throw new InvalidOperationException( "Usage session has ended." ) )
        {
            this._logger.Trace.Log( $"  {metric.Name}: {metric}" );
        }
    }
}