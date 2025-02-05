// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Licensing.Licenses
{
    /// <summary>
    /// Creates an <see cref="ILicense" /> object from a license string.
    /// </summary>
    public class LicenseFactory
    {
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseFactory"/> class.
        /// </summary>
        /// <param name="services">Services.</param>
        public LicenseFactory( IServiceProvider services )
        {
            this._services = services;
            this._logger = services.GetLoggerFactory().Licensing();
        }

        /// <summary>
        /// Attempts to create an <see cref="ILicense" /> object from a license string.
        /// </summary>
        /// <param name="licenseString">The license string. E.g. license key or license server URL.</param>
        /// <param name="license">The <see cref="ILicense" /> object represented by the <paramref name="licenseString"/>.</param>
        /// <param name="errorMessage">Description of a failure when the return value is <c>false</c>.</param>
        /// <returns>A value indicating if the <paramref name="licenseString"/> represents a license.</returns>
        public bool TryCreate( string? licenseString, [MaybeNullWhen( false )] out ILicense license, [MaybeNullWhen( true )] out string errorMessage )
        {
            licenseString = licenseString?.Trim();

            if ( string.IsNullOrEmpty( licenseString ) )
            {
                errorMessage = "Empty license string provided.";
                this._logger.Error?.Log( errorMessage );
                license = null;

                return false;
            }

            if ( Uri.IsWellFormedUriString( licenseString, UriKind.Absolute ) )
            {
                // TODO License Server Support
                errorMessage = "License server is not yet supported.";
                this._logger.Error?.Log( errorMessage );
                license = null;

                return false;
            }
            else
            {
                // ReSharper disable once RedundantSuppressNullableWarningExpression
                license = new License( licenseString!, this._services );
                errorMessage = null;

                return true;
            }
        }
    }
}