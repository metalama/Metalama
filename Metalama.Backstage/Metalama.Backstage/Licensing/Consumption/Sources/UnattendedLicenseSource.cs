// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Licensing.Registration;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Licensing.Consumption.Sources;

internal class UnattendedLicenseSource : ILicenseSource, ILicense
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly IApplicationInfo _applicationInfo;

    public string Description => "unattended license source";

    public LicenseSourceKind Kind => LicenseSourceKind.Unattended;

    public UnattendedLicenseSource( IServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._applicationInfo = serviceProvider.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication;
        this._logger = serviceProvider.GetLoggerFactory().Licensing();
    }

    public ILicense? GetLicense( Action<LicensingMessage> reportMessage )
    {
        if ( this._applicationInfo.IsUnattendedProcess( this._serviceProvider.GetLoggerFactory() ) )
        {
            this._logger.Trace?.Log( "Providing an unattended process license." );

            return this;
        }
        else
        {
            this._logger.Trace?.Log( "The process is attended. Not providing an unattended process license." );

            return null;
        }
    }

    bool ILicense.TryGetLicenseConsumptionData(
        [MaybeNullWhen( false )] out LicenseConsumptionData licenseConsumptionData,
        [MaybeNullWhen( true )] out string errorMessage )
    {
        licenseConsumptionData = new LicenseConsumptionData(
            LicensedProduct.MetalamaUltimate,
            LicenseType.Unattended,
            null,
            "Unattended Process License",
            new Version( 0, 0 ),
            null,
            false,
            false );

        errorMessage = null;

        return true;
    }

    bool ILicense.TryGetProperties(
        [MaybeNullWhen( false )] out LicenseProperties licenseProperties,
        [MaybeNullWhen( true )] out string errorMessage )
        => throw new NotSupportedException( "Unattended license source doesn't support license registration." );

    event Action? ILicenseSource.Changed { add { } remove { } }

    public LicenseSourcePriority Priority => LicenseSourcePriority.Unattended;
}