// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// TODO

// using Metalama.Backstage.Extensibility;
// using System;
// using System.Globalization;
// using System.IO;
// using System.Net;

namespace Metalama.Backstage.Licensing.Consumption.LicenseServer
{
    internal sealed class LicenseServerClient
    {
// private readonly LicenseLeaseCache _cache;
//        private readonly LicenseeIdentifier _licenseeIdentifier;
//        private readonly IWebClientService _webClientService;
//        private readonly IDateTimeProvider _dateTimeProvider;
//        private readonly IApplicationInfoService _applicationInfoService;
//        private readonly ITrace _licensingTrace;

// public LicenseServerClient( LicenseLeaseCache cache, LicenseeIdentifier licenseeIdentifier, IWebClientService webClientService, IDateTimeProvider dateTimeProvider, IApplicationInfoService applicationInfoService, ITrace licensingTrace )
//        {
//            this._cache = cache;
//            this._licenseeIdentifier = licenseeIdentifier;
//            this._webClientService = webClientService;
//            this._dateTimeProvider = dateTimeProvider;
//            this._applicationInfoService = applicationInfoService;
//            this._licensingTrace = licensingTrace;
//        }

// /// <exclude/>
//        internal static bool IsLicenseServerUrl( string licenseString )
//        {
//            string message;
//            return IsLicenseServerUrl( licenseString, out message );
//        }

// /// <exclude/>
//        internal static bool IsLicenseServerUrl( string licenseString, out string errorMessage )
//        {
//            if ( !Uri.IsWellFormedUriString( licenseString, UriKind.Absolute ) )
//            {
//                errorMessage = "Invalid URL.";
//                return false;
//            }

// Uri uri = new Uri( licenseString, UriKind.Absolute );

// if ( uri.Scheme != Uri.UriSchemeHttp &&
//                 uri.Scheme != Uri.UriSchemeHttps )
//            {
//                errorMessage = "Only HTTP and HTTPS are acceptable protocols.";
//                return false;
//            }

// if ( !string.IsNullOrEmpty( uri.Query ) )
//            {
//                errorMessage = "The URL cannot contain a query string.";
//                return false;
//            }

// errorMessage = null;
//            return true;
//        }

// internal bool TryDownloadLease( string url, out LicenseLease lease, IDiagnosticsSink diagnosticsSink, bool renewal = false )
//        {
//            try
//            {
//                lease = this.DownloadLease( url );

// if (!License.TryDeserialize(lease.LicenseString, this._applicationInfoService, out _, this._licensingTrace))
//                {
//                    return false;
//                }

// this._cache.Cache( lease );
//                return true;
//            }
//            catch ( Exception e )
//            {
//                diagnosticsSink?.ReportError( $"Cannot get a lease from the license server: {e.Message}." ); // PS0148

// // Return null to use the last-known good lease.
//                lease = null;
//                return false;
//            }
//        }

// private LicenseLease DownloadLease( string url )
//        {
//            if ( !url.Contains( "?" ) )
//            {
//                url = url.TrimEnd( '/' );

// url += string.Format(
//                    CultureInfo.InvariantCulture,
//                    "/Lease.ashx?user={0}&machine={1}-{2:x}&version={3}&buildDate={4:o}",
//                    Environment.UserName,
//                    Environment.MachineName,
//                    this._licenseeIdentifier.GetCurrentMachineHash(),
//                    this._applicationInfoService.VersionString,
//                    this._applicationInfoService.BuildDate );
//            }

// this._licensingTrace?.WriteLine( "Leasing a license from the server: {0}.", url );

// string leaseString;

// try
//            {
//                leaseString = this._webClientService.DownloadString( url );
//            }
//            catch ( WebException e )
//            {
//                HttpWebResponse response = (HttpWebResponse) e.Response;
//                if ( response != null && response.StatusCode == HttpStatusCode.Forbidden /* Forbidden */ )
//                {
//                    StreamReader reader = new StreamReader( response.GetResponseStream() );
//                    throw new InvalidLicenseException( reader.ReadToEnd(), e );
//                }
//                throw;
//            }

// if ( !LicenseLease.TryDeserialize( leaseString, this._dateTimeProvider, out var lease ) )
//            {
//                throw new InvalidLicenseException( "The license server returned an invalid response." );
//            }

// return lease;
//        }

// /// <exclude/>
//        internal bool TestLicenseServer( string url, out string? errorMessage, out License? license )
//        {
//            try
//            {
//                var lease = this.DownloadLease( url );

// if (!License.TryDeserialize(lease.LicenseString, this._applicationInfoService, out license, this._licensingTrace ))
//                {
//                    errorMessage = "The downloaded lease is invalid.";
//                    return false;
//                }

// if ( !license.Validate( null, this._dateTimeProvider, out errorMessage ) )
//                {
//                    return false;
//                }

// errorMessage = null;
//                return true;
//            }
//            catch ( Exception e )
//            {
//                errorMessage = e.Message;
//                license = null;
//                return false;
//            }
//        }
    }
}