// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Consumption.Sources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Backstage.Licensing.Consumption;

/// <inheritdoc />
internal sealed class LicenseConsumptionService : ILicenseConsumptionService
{
    private readonly IServiceProvider _services;
    private readonly IReadOnlyList<ILicenseSource> _sources;

    public LicenseConsumptionService( IServiceProvider services, IReadOnlyList<ILicenseSource> licenseSources )
    {
        this._services = services;
        this._sources = licenseSources;

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
            sources.Add( new ExplicitLicenseSource( options.ProjectLicenseKey!, LicenseSourceKind.Project, this._services ) );
        }

        return LicenseConsumer.Create( options, this._services, sources, reportMessage );
    }

    public event Action? Changed;
}