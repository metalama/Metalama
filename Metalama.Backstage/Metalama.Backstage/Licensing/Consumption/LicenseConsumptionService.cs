// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Licensing.Licenses;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Backstage.Licensing.Consumption;

/// <inheritdoc />
internal class LicenseConsumptionService : ILicenseConsumptionService
{
    private readonly IServiceProvider _services;
    private readonly IReadOnlyList<ILicenseSource> _sources;
    private readonly ConcurrentDictionary<string, NamespaceLicenseInfo> _embeddedRedistributionLicensesCache = [];
    private readonly LicenseFactory _licenseFactory;

    public LicenseConsumptionService( IServiceProvider services, IReadOnlyList<ILicenseSource> licenseSources )
    {
        this._services = services;
        this._sources = licenseSources;
        this._licenseFactory = new LicenseFactory( services );

        foreach ( var source in this._sources )
        {
            source.Changed += this.OnSourceChanged;
        }
    }

    private void OnSourceChanged()
    {
        this.Changed?.Invoke();
    }

    public ILicenseConsumer CreateConsumer(
        string? projectLicenseKey,
        LicenseSourceKind ignoredLicenseKinds,
        out ImmutableArray<LicensingMessage> messages )
    {
        var sources = new List<ILicenseSource>( this._sources.Count + 1 );

        sources.AddRange( this._sources.Where( s => (s.Kind & ignoredLicenseKinds) == 0 ) );

        if ( !string.IsNullOrEmpty( projectLicenseKey ) )
        {
            // ReSharper disable once RedundantSuppressNullableWarningExpression
            sources.Add( new ExplicitLicenseSource( projectLicenseKey!, this._services ) );
        }

        return LicenseConsumer.Create( this._services, sources, out messages );
    }

    public ILicenseConsumer CreateConsumer( string? projectLicenseKey = null, LicenseSourceKind ignoredLicenseKinds = LicenseSourceKind.None )
        => this.CreateConsumer( projectLicenseKey, ignoredLicenseKinds, out _ );

    public bool TryValidateRedistributionLicenseKey( string redistributionLicenseKey, string aspectClassNamespace, out ImmutableArray<LicensingMessage> errors )
    {
        if ( !this._embeddedRedistributionLicensesCache.TryGetValue( redistributionLicenseKey, out var licensedNamespace ) )
        {
            if ( !this._licenseFactory.TryCreate( redistributionLicenseKey, out var license, out var errorMessage ) )
            {
                errors = ImmutableArray.Create( new LicensingMessage( errorMessage ) { IsError = true } );

                return false;
            }

            if ( !license.TryGetLicenseConsumptionData( out var licenseConsumptionData, out errorMessage ) )
            {
                errors = ImmutableArray.Create( new LicensingMessage( errorMessage ) { IsError = true } );

                return false;
            }

            if ( !licenseConsumptionData.IsRedistributable )
            {
                errors = ImmutableArray<LicensingMessage>.Empty;

                return false;
            }

            if ( string.IsNullOrEmpty( licenseConsumptionData.LicensedNamespace ) )
            {
                errors = ImmutableArray<LicensingMessage>.Empty;

                return false;
            }

            licensedNamespace = new NamespaceLicenseInfo( licenseConsumptionData.LicensedNamespace! );
            this._embeddedRedistributionLicensesCache.TryAdd( redistributionLicenseKey, licensedNamespace );
        }

        errors = ImmutableArray<LicensingMessage>.Empty;

        return licensedNamespace.AllowsNamespace( aspectClassNamespace );
    }

    public event Action? Changed;
}