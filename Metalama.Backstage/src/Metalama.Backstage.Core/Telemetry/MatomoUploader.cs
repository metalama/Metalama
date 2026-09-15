// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Metalama.Backstage.Telemetry;

internal sealed class MatomoUploader : IBackstageService
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RandomNumberGenerator _randomNumberGenerator;
    private readonly TelemetryLogger _telemetryLogger;
    private readonly Uri? _analyticsUri;
    private readonly string _productName;

    public MatomoUploader( IServiceProvider serviceProvider )
    {
        this._analyticsUri = serviceProvider.GetRequiredBackstageService<TelemetryInitializationOptions>().AnalyticsUri;
        this._productName = serviceProvider.GetRequiredBackstageService<ProductProfile>().Name;
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "Metrics" );
        this._httpClientFactory = serviceProvider.GetRequiredBackstageService<IHttpClientFactory>();
        this._randomNumberGenerator = serviceProvider.GetRequiredBackstageService<RandomNumberGenerator>();
        this._telemetryLogger = serviceProvider.GetRequiredBackstageService<TelemetryLogger>();
    }

    public Task SendUsageAuditAsync( UsageTelemetryReport report ) => this.SendAsync( report, "usage", newVisit: true );

    /// <summary>
    /// Sends an event to the analytics endpoint. The dimensions 3 (product), 4 (version) and 5 (device age) are
    /// always sent; a caller adds its own dimensions, for instance the licensed product of an audit.
    /// </summary>
    /// <param name="report">The report that gives the device hash, the version and the device age.</param>
    /// <param name="actionName">The name of the action, for instance <c>usage</c> or <c>license</c>.</param>
    /// <param name="newVisit">A value indicating whether the event starts a new visit.</param>
    /// <param name="dimensions">Additional dimensions, given by their index and value.</param>
    public Task SendAsync( TelemetryReport report, string actionName, bool newVisit, params (int Index, string Value)[] dimensions )
    {
        if ( this._analyticsUri == null )
        {
            return Task.CompletedTask;
        }

        var reportedVersion = report.AssemblyVersion?.ToString( 2 );

        var request = new StringBuilder();

        request.Append( this._analyticsUri.AbsoluteUri )
            .Append( "&rec=1" )
            .Append( "&action_name=" ).Append( actionName )
            .Append( "&_id=" ).Append( report.AggregateTrackingDeviceHash.ToString( "x", CultureInfo.InvariantCulture ) )
            .Append( "&uid=" ).Append( report.AggregateTrackingDeviceHash.ToString( "x", CultureInfo.InvariantCulture ) );

        foreach ( var dimension in dimensions )
        {
            request.Append( "&dimension" ).Append( dimension.Index.ToString( CultureInfo.InvariantCulture ) ).Append( '=' ).Append( dimension.Value );
        }

        request.Append( "&dimension3=" ).Append( this._productName )
            .Append( "&dimension4=" ).Append( reportedVersion )
            .Append( "&dimension5=" ).Append( report.DeviceAgeBucket.ToString() )
            .Append( "&new_visit=" ).Append( newVisit ? '1' : '0' )
            .Append( "&rand=" ).Append( this._randomNumberGenerator.NextInt64().ToString( "x", CultureInfo.InvariantCulture ) );

        return this.SendData( request.ToString() );
    }

    private async Task SendData( string url )
    {
        try
        {
            var http = this._httpClientFactory.Create();

            var response = await http.GetAsync( url );

            this._telemetryLogger.WriteLine( $"'{url}': {response.ReasonPhrase}." );

            if ( !response.IsSuccessStatusCode )
            {
                this._logger.Warning?.Log( $"License audit to Matomo returned {response.ReasonPhrase}." );
            }
        }
        catch ( Exception e )
        {
            this._logger.LogException( e, "Cannot audit to Matomo" );
        }
    }
}