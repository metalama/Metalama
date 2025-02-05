// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using K4os.Hash.xxHash;
using Metalama.Backstage.Application;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Telemetry.Metrics;
using System;
using System.Globalization;
using System.Text;

namespace Metalama.Backstage.Licensing.Audit;

internal class LicenseAuditTelemetryReport : TelemetryReport
{
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;
    private readonly IUsageReporter _usageReporter;

    public LicenseConsumptionData License { get; }

    public IComponentInfo ReportedComponent { get; }

    public override string Kind => "LicenseAudit";

    public DateTime BuildDate { get; }

#pragma warning disable CA1822
    public long UserHash => LicenseCryptography.ComputeStringHash64( Environment.UserName );
#pragma warning restore CA1822

    public long DeviceHash => LicenseCryptography.ComputeStringHash64( this._telemetryConfigurationService.DeviceId.ToString() );

    public DateTime Date { get; }

    public Version? AssemblyVersion => this.ReportedComponent.AssemblyVersion;

    public string ApplicationName => this.ReportedComponent.Name;

    public bool IsUsageReportingEnabled => this._usageReporter.IsUsageReportingEnabled;

    public long AuditHashCode { get; }

    public LicenseAuditTelemetryReport(
        IServiceProvider serviceProvider,
        LicenseConsumptionData license )
    {
        this.License = license;
        this.Date = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>().UtcNow;

        this.ReportedComponent = serviceProvider
            .GetRequiredBackstageService<IApplicationInfoProvider>()
            .CurrentApplication
            .GetLatestComponentMadeByPostSharp();

        this._usageReporter = serviceProvider.GetRequiredBackstageService<IUsageReporter>();

        this.BuildDate = this.ReportedComponent.BuildDate
                         ?? throw new InvalidOperationException( $"Build date of '{this.ReportedComponent.Name}' application is unknown." );

        this._telemetryConfigurationService = serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();

        var auditHashCodeBuilder = new XXH64();

        void AddToMetricsAndHashCode( Metric metric )
        {
            this.Metrics.Add( metric );
            
            // ReSharper disable once RedundantSuppressNullableWarningExpression
            auditHashCodeBuilder.Update( Encoding.UTF8.GetBytes( metric.ToString()! ) );
        }

        // Audit date is not part of the audit hash code. 
        this.Metrics.Add( new LicenseAuditDateMetric( "Date", this.Date ) );
        AddToMetricsAndHashCode( new StringMetric( "Version", this.ReportedComponent.PackageVersion ) );
        AddToMetricsAndHashCode( new LicenseAuditDateMetric( "BuildDate", this.BuildDate ) );
        AddToMetricsAndHashCode( new StringMetric( "License", this.License.LicenseString ) );
        AddToMetricsAndHashCode( new LicenseAuditHashMetric( "User", this.UserHash ) );
        AddToMetricsAndHashCode( new LicenseAuditHashMetric( "Machine", this.DeviceHash ) );
        AddToMetricsAndHashCode( new BoolMetric( "CEIP", this.IsUsageReportingEnabled ) );
        AddToMetricsAndHashCode( new StringMetric( "ApplicationName", this.ApplicationName ) );

        this.AuditHashCode = unchecked((long) auditHashCodeBuilder.Digest());
    }

    /// <summary>
    /// Date metric implementation based on
    /// PostSharp.Hosting.Program.WriteLicenseAudit method.
    /// </summary>
    private class LicenseAuditDateMetric : Metric
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
    private class LicenseAuditHashMetric : Metric
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