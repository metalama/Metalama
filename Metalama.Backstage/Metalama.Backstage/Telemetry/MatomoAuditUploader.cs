// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Audit;
using System;
using System.Threading.Tasks;

namespace Metalama.Backstage.Telemetry;

internal sealed class MatomoAuditUploader : IBackstageService
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RandomNumberGenerator _randomNumberGenerator;
    private readonly TelemetryLogger _telemetryLogger;

    public MatomoAuditUploader( IServiceProvider serviceProvider )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "Metrics" );
        this._httpClientFactory = serviceProvider.GetRequiredBackstageService<IHttpClientFactory>();
        this._randomNumberGenerator = serviceProvider.GetRequiredBackstageService<RandomNumberGenerator>();
        this._telemetryLogger = serviceProvider.GetRequiredBackstageService<TelemetryLogger>();
    }

    public async Task UploadAsync( LicenseAuditTelemetryReport report )
    {
        var http = this._httpClientFactory.Create();

        var licensedProduct = report.License.LicenseProduct switch
        {
            LicenseProduct.PostSharpFramework => "PostSharpFramework",
            LicenseProduct.PostSharpUltimate => "PostSharpUltimate",
            _ => report.License.LicenseProduct.ToString()
        };

        var licenseType = report.License.LicenseType switch
        {
            // Avoid ambiguities due to duplicate names.
            LicenseType.Business => nameof(LicenseType.Business),
            LicenseType.Community => nameof(LicenseType.Community),
            _ => report.License.LicenseType.ToString()
        };

        // Note that we are intentionally and "randomly" reporting the version of the first component that
        // triggered audit, to prioritize having just one hit per day over having accurate version reporting
        // (at least for Matomo reporting).
        var reportedVersion = report.AssemblyVersion?.ToString( 2 );

        var request =
#pragma warning disable CA1307
            $"https://postsharp.matomo.cloud/matomo.php?idsite=6"
            + $"&rec=1"
            + $"&_id={report.DeviceHash:x}"
            + $"&uid={report.DeviceHash:x}"
            + $"&dimension1={licensedProduct}"
            + $"&dimension2={licenseType}"
            + $"&dimension3=Metalama"
            + $"&dimension4={reportedVersion}"
            + $"&new_visit=1"
            + $"&rand={this._randomNumberGenerator.NextInt64():x}";
#pragma warning restore CA1307

        try
        {
            var response = await http.GetAsync( request );

            this._telemetryLogger.WriteLine( $"'{request}': {response.ReasonPhrase}." );

            if ( !response.IsSuccessStatusCode )
            {
                this._logger.Warning?.Log( $"License audit to Matomo returned {response.ReasonPhrase}." );
            }
        }
        catch ( Exception e )
        {
            this._logger.Error?.Log( $"Cannot audit to Matomo: {e.Message}" );
        }
    }
}