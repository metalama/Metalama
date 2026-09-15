// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Audit;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Registration;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Licensing.Licenses;

internal abstract class AuditableLicense : ILicense
{
    private readonly ILicenseAuditManager? _licenseAuditManager;
    private readonly BackstageBackgroundTasksService _backgroundTasksService;

    protected ILogger Logger { get; }

    internal AuditableLicense( IServiceProvider services )
    {
        this._licenseAuditManager = services.GetBackstageService<ILicenseAuditManager>();
        this._backgroundTasksService = services.GetRequiredBackstageService<BackstageBackgroundTasksService>();
        this.Logger = services.GetLoggerFactory().Licensing();
    }

    public abstract bool CanBeRegistered( [MaybeNullWhen( true )] out string errorMessage );

    public abstract bool TryGetConsumptionProperties(
        LicenseConsumptionOptions options,
        [MaybeNullWhen( false )] out LicenseConsumptionProperties licenseConsumptionProperties,
        [MaybeNullWhen( true )] out string errorMessage );

    public abstract bool TryGetRegistrationProperties(
        [MaybeNullWhen( false )] out LicenseRegistrationProperties licenseProperties,
        [MaybeNullWhen( true )] out string errorMessage );

    public void ReportUse()
    {
        if ( this._licenseAuditManager != null )
        {
            if ( this.TryGetConsumptionProperties( LicenseConsumptionOptions.Default, out var properties, out var errorMessage ) )
            {
                if ( properties.IsAuditable )
                {
                    this._backgroundTasksService.Enqueue( () => this._licenseAuditManager.ReportLicense( properties ) );
                }
                else
                {
                    this.Logger.Warning?.Log( $"License audit is skipped: the license is not auditable." );
                }
            }
            else
            {
                this.Logger.Warning?.Log( $"License audit is skipped: {errorMessage}" );
            }
        }
        else
        {
            this.Logger.Warning?.Log( $"License audit is skipped because there is no {nameof(ILicenseAuditManager)}." );
        }
    }
}