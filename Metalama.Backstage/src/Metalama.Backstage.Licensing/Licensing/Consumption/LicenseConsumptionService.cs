// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Licensing.Registration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace Metalama.Backstage.Licensing.Consumption;

/// <inheritdoc />
internal sealed class LicenseConsumptionService : ILicenseConsumptionService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReadOnlyList<ILicenseSource> _sources;
    private readonly ILogger _logger;

    public LicenseConsumptionService( IServiceProvider serviceProvider, IReadOnlyList<ILicenseSource> licenseSources )
    {
        this._serviceProvider = serviceProvider;
        this._sources = licenseSources;
        this._logger = this._serviceProvider.GetLoggerFactory().Licensing();

        foreach ( var source in this._sources )
        {
            source.Changed += this.OnSourceChanged;
        }
    }

    private void OnSourceChanged()
    {
        this.Changed?.Invoke();
    }

    public ILicenseConsumer CreateConsumer( LicenseConsumptionOptions? options, Action<LicensingMessage>? reportMessage )
    {
        options ??= LicenseConsumptionOptions.Default;

        var sources = new List<ILicenseSource>( this._sources.Count + 1 );

        sources.AddRange( this._sources.Where( s => (s.Kind & options.IgnoredLicenseSources) == 0 ) );

        if ( !string.IsNullOrEmpty( options.ProjectLicenseKey ) )
        {
            // ReSharper disable once RedundantSuppressNullableWarningExpression
            sources.Add( new ExplicitLicenseSource( options.ProjectLicenseKey!, LicenseSourceKind.Project, this._serviceProvider ) );
        }

        return this.CreateConsumer( options, sources, reportMessage );
    }

    private ILicenseConsumer CreateConsumer(
        LicenseConsumptionOptions options,
        IEnumerable<ILicenseSource> licenseSources,
        Action<LicensingMessage>? reportMessage = null )
    {
        // Gather valid licenses.
        var licenses = licenseSources.OrderBy( s => s.Priority ).SelectMany( s => s.GetLicenses( ReportMessage ).Select( l => (License: l, Source: s) ) );

        var validLicenses = ImmutableArray.CreateBuilder<(ILicense License, LicenseConsumptionProperties Properties)>();

        foreach ( var license in licenses )
        {
            if ( !license.License.TryGetConsumptionProperties( options, out var licenseConsumptionData, out var errorMessage ) )
            {
                LicenseRegistrationProperties? registrationData = null;

                if ( license.Source.SupportsRegistration )
                {
                    license.License.TryGetRegistrationProperties( out registrationData, out _ );
                }

                var message =
                    $"Cannot use the license '{registrationData?.LicenseId?.ToString( CultureInfo.InvariantCulture ) ?? registrationData?.Description}': {errorMessage}"
                        .TrimEnd( '.' ) + ".";

                if ( license.Source.GetType() != typeof(UserProfileLicenseSource) )
                {
                    message += $" The license key originates from {license.Source.Description}.";
                }

                ReportMessage( new LicensingMessage( message ) );

                continue;
            }

            validLicenses.Add( (license.License, licenseConsumptionData) );
        }

        // Return the LicenseConsumer.
        return new LicenseConsumer( this._serviceProvider, validLicenses.ToImmutableArray(), options );

        void ReportMessage( LicensingMessage message )
        {
            reportMessage?.Invoke( message );
            this._logger.Warning?.Log( message.Text );
        }
    }

    public event Action? Changed;
}