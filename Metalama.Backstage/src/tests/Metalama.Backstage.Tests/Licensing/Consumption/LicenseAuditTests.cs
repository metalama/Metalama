// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Audit;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Tests.Licensing.Licenses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Consumption;

public sealed class LicenseAuditTests : LicenseConsumptionServiceTestsBase
{
    private static readonly string _auditedLicenseKey = LicenseKeyProvider.MetalamaProfessionalBusiness;

    public LicenseAuditTests( ITestOutputHelper logger ) : base( logger, isTelemetryEnabled: true ) { }

    protected override void ConfigureServices( ServiceProviderBuilder services )
    {
        base.ConfigureServices( services );

        services
            .AddSingleton( serviceProvider => new TelemetryLogger( serviceProvider ) )
            .AddSingleton<ITelemetryUploader>( new NullTelemetryUploader() )
            .AddSingleton<IUsageReporter>( new NullUsageReporter() )
            .AddSingleton<ILicenseAuditManager>( serviceProvider => new LicenseAuditManager( serviceProvider ) )
            .AddSingleton<TelemetryReportUploader>( serviceProvider => new TelemetryReportUploader( serviceProvider ) )
            .AddSingleton( serviceProvider => new MatomoUploader( serviceProvider ) );
    }

    private InstrumentedLicenseWrapper CreateAndConsumeLicense( string licenseKey )
    {
        var license = this.CreateInstrumentedLicenseWrapper( licenseKey );
        var consumer = this.CreateConsumptionService( license ).CreateConsumer();
        Assert.True( consumer.TryConsume( LicenseRequirement.Any ) );

        return license;
    }

    private string[] GetReports()
    {
        this.BackgroundTasks.WhenNoPendingTaskAsync().Wait();

        var files = this.FileSystem.Mock.AllFiles.Where( path => Path.GetFileName( path ).StartsWith( "LicenseAudit-", StringComparison.Ordinal ) );
        var reports = files.SelectMany( f => this.FileSystem.ReadAllLines( f ) ).ToArray();

        return reports;
    }

    [Theory]
    [InlineData( nameof(LicenseKeyProvider.MetalamaProfessionalBusiness), true, "MetalamaProfessional", "Business" )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaProfessionalBusinessNotAuditable), false, null, null )]
    public void LicenseIsAudited( string licenseKeyName, bool isAuditReportExpected, string? expectedProductName, string? expectedLicenseType )
    {
        var licenseKey = LicenseKeyProvider.GetLicenseKey( licenseKeyName );

        void Consume()
        {
            _ = this.CreateAndConsumeLicense( licenseKey );
        }

        if ( isAuditReportExpected )
        {
            Consume();
            var reports = this.GetReports();
            Assert.Single( reports );

            Assert.Contains( licenseKey, reports[0], StringComparison.OrdinalIgnoreCase );
            var (matomoRequest, _) = Assert.Single( this.HttpClientFactory.ProcessedRequests, r => r.Request.RequestUri?.Host == "postsharp.matomo.cloud" );
            var matomoRequestUri = matomoRequest.RequestUri?.ToString();

            this.Logger.WriteLine( matomoRequestUri );

            Assert.Equal( HttpMethod.Get, matomoRequest.Method );

            Assert.Equal(
                $"https://postsharp.matomo.cloud/matomo.php?idsite=6&rec=1&action_name=license&_id=412522694e2c0786&uid=412522694e2c0786&dimension1={expectedProductName}&dimension2={expectedLicenseType}&dimension3=Metalama&dimension4=1.0&new_visit=0&rand=56addf3428448b3b",
                matomoRequestUri );

            // Second time in the same day.
            this.FileSystem.Reset();

            Consume();
            var secondReports = this.GetReports();
            Assert.Empty( secondReports );

            // Third time, one day later.
            this.FileSystem.Reset();
            this.HttpClientFactory.Reset();
            this.Time.AddTime( TimeSpan.FromDays( 1.01 ) );

            Consume();
            var thirdReports = this.GetReports();
            Assert.Single( thirdReports );

            var (thirdMatomoRequest, _) = Assert.Single(
                this.HttpClientFactory.ProcessedRequests,
                r => r.Request.RequestUri?.Host == "postsharp.matomo.cloud" );

            var thirdMatomoRequestUri = thirdMatomoRequest.RequestUri?.ToString();

            this.Logger.WriteLine( thirdMatomoRequestUri );

            Assert.Equal( HttpMethod.Get, thirdMatomoRequest.Method );

            Assert.Equal(
                $"https://postsharp.matomo.cloud/matomo.php?idsite=6&rec=1&action_name=license&_id=412522694e2c0786&uid=412522694e2c0786&dimension1={expectedProductName}&dimension2={expectedLicenseType}&dimension3=Metalama&dimension4=1.0&new_visit=0&rand=689070376c8cf5f8",
                thirdMatomoRequestUri );
        }
        else
        {
            Assert.Empty( this.FileSystem.Mock.AllFiles );
        }
    }

    [Fact]
    public void LicenseAuditReportsDistinctLicenseKeyWithNoDelay()
    {
        var licenseKeys = new List<string> { LicenseKeyProvider.MetalamaProfessionalBusiness, LicenseKeyProvider.MetalamaProfessionalPersonal };

        licenseKeys.ForEach( l => this.CreateAndConsumeLicense( l ) );

        var reports = this.GetReports();

        Assert.Equal( licenseKeys.Count, reports.Length );

        foreach ( var t in licenseKeys )
        {
#pragma warning disable CA1307
            Assert.Contains( reports, r => r.Contains( t ) );
#pragma warning restore CA1307
        }
    }

    private void AssertReportsCount( int expectedCount )
    {
        var reports = this.GetReports();
        Assert.Equal( expectedCount, reports.Length );
        Assert.All( reports, r => Assert.Contains( _auditedLicenseKey, r, StringComparison.OrdinalIgnoreCase ) );
    }

    private void ConsumeAndAssertReportsCount( int expectedCount )
    {
        this.CreateAndConsumeLicense( _auditedLicenseKey );
        this.AssertReportsCount( expectedCount );
    }

    [Fact]
    public void LicenseAuditReportsSameLicenseKeyDaily()
    {
        Assert.Empty( this.FileSystem.Mock.AllFiles );

        var now = new DateTime( 2022, 01, 01, 0, 0, 0, DateTimeKind.Utc );

        void ShiftTime( TimeSpan span )
        {
            now += span;
            this.Time.Set( now );
        }

        ShiftTime( TimeSpan.Zero );
        this.ConsumeAndAssertReportsCount( 1 );

        this.ConsumeAndAssertReportsCount( 1 );

        ShiftTime( TimeSpan.FromDays( 1 ) - TimeSpan.FromMilliseconds( 1 ) );
        this.ConsumeAndAssertReportsCount( 1 );

        ShiftTime( TimeSpan.FromMilliseconds( 1 ) );
        this.ConsumeAndAssertReportsCount( 2 );

        this.ConsumeAndAssertReportsCount( 2 );

        ShiftTime( TimeSpan.FromDays( 1 ) - TimeSpan.FromMilliseconds( 1 ) );
        this.ConsumeAndAssertReportsCount( 2 );

        ShiftTime( TimeSpan.FromMilliseconds( 1 ) );
        this.ConsumeAndAssertReportsCount( 3 );
    }

    [Fact]
    public void DeviceIdIsSerialized()
    {
        var guid = new Guid( "75c1ce19-e594-4bfe-ac39-e37b9dd62069" );
        var configuration = new TelemetryConfiguration { DeviceId = guid };
        var json = configuration.ToJson();
        Assert.Contains( guid.ToString(), json, StringComparison.Ordinal );
    }

    [Fact]
    public void LicenseIsNotReportedReportedWhenTelemetryIsDisabled()
    {
        this.ApplicationInfo = new TestApplicationInfo() { IsTelemetryEnabled = false };
        this.ConsumeAndAssertReportsCount( 0 );
    }

    [Fact]
    public void LicenseIsReportedWhenOptOutEnvironmentVariableIsSet()
    {
        this.EnvironmentVariableProvider.Environment["METALAMA_TELEMETRY_OPT_OUT"] = "true";
        this.ConsumeAndAssertReportsCount( 1 );
    }

    [Fact]
    public void LicenseIsNotReportedForUnattendedBuild()
    {
        this.ApplicationInfo = new TestApplicationInfo() { IsUnattendedProcess = true };
        this.ConsumeAndAssertReportsCount( 0 );
    }
}