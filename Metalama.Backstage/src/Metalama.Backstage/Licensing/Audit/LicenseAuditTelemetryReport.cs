// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.IO.Hashing;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Telemetry.Metrics;
using System;
using System.Globalization;
using System.Text;

namespace Metalama.Backstage.Licensing.Audit;

internal sealed class LicenseAuditTelemetryReport : TelemetryReport
{
    private readonly IUsageReporter _usageReporter;

    public LicenseConsumptionProperties License { get; }

    public override string Kind => "LicenseAudit";

    private bool IsUsageReportingEnabled => this._usageReporter.IsUsageReportingEnabled;

    public long AuditHashCode { get; }

    public LicenseAuditTelemetryReport(
        IServiceProvider serviceProvider,
        LicenseConsumptionProperties license ) : base( serviceProvider, new MetricCollection() )
    {
        this.License = license;
        var date = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>().UtcNow;

        this._usageReporter = serviceProvider.GetRequiredBackstageService<IUsageReporter>();

        var buildDate = this.ReportedComponent.BuildDate
                        ?? throw new InvalidOperationException( $"Build date of '{this.ReportedComponent.Name}' application is unknown." );

        var auditHashCodeBuilder = new XxHash64();

        void AddToMetricsAndHashCode( Metric metric )
        {
            this.Metrics.Add( metric );

            // ReSharper disable once RedundantSuppressNullableWarningExpression
            auditHashCodeBuilder.Append( Encoding.UTF8.GetBytes( metric.ToString()! ) );
        }

        // Audit date is not part of the audit hash code. 
        this.Metrics.Add( new LicenseAuditDateMetric( "Date", date ) );
        AddToMetricsAndHashCode( new StringMetric( "Version", this.ReportedComponent.PackageVersion ) );
        AddToMetricsAndHashCode( new LicenseAuditDateMetric( "BuildDate", buildDate ) );
        AddToMetricsAndHashCode( new StringMetric( "License", this.License.LicenseString ) );
        AddToMetricsAndHashCode( new LicenseAuditHashMetric( "User", this.UserHash ) );
        AddToMetricsAndHashCode( new LicenseAuditHashMetric( "Machine", this.DeviceHash ) );
        AddToMetricsAndHashCode( new BoolMetric( "CEIP", this.IsUsageReportingEnabled ) );
        AddToMetricsAndHashCode( new StringMetric( "ApplicationName", this.ApplicationName ) );

        this.AuditHashCode = unchecked((long) auditHashCodeBuilder.GetCurrentHashAsUInt64());
    }

    /// <summary>
    /// Date metric implementation based on
    /// PostSharp.Hosting.Program.WriteLicenseAudit method.
    /// </summary>
    private sealed class LicenseAuditDateMetric : Metric
    {
        private readonly DateTime _value;

        public LicenseAuditDateMetric( string name, DateTime value )
            : base( name )
        {
            this._value = value;
        }

        public override string ToString() => this._value.ToString( "d", CultureInfo.InvariantCulture );

        public override bool SetValue( object? value ) => throw new NotImplementedException();
    }

    /// <summary>
    /// Hash metric implementation based on
    /// PostSharp.Hosting.Program.WriteLicenseAudit method.
    /// </summary>
    private sealed class LicenseAuditHashMetric : Metric
    {
        private readonly long _value;

        public LicenseAuditHashMetric( string name, long value )
            : base( name )
        {
            this._value = value;
        }

        public override string ToString() => this._value.ToString( "x", CultureInfo.InvariantCulture );

        public override bool SetValue( object? value ) => throw new NotImplementedException();
    }
}