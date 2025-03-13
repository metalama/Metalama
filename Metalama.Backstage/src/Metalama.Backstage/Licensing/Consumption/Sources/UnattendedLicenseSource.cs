// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Licensing.Registration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Licensing.Consumption.Sources;

internal sealed class UnattendedLicenseSource : ILicenseSource, ILicense
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

    public IEnumerable<ILicense> GetLicenses( Action<LicensingMessage> reportMessage )
    {
        if ( this._applicationInfo.IsUnattendedProcess( this._serviceProvider.GetLoggerFactory() ) )
        {
            this._logger.Trace?.Log( "Providing an unattended process license." );

            return [this];
        }
        else
        {
            this._logger.Trace?.Log( "The process is attended. Not providing an unattended process license." );

            return [];
        }
    }

    public bool CanBeRegistered( [MaybeNullWhen( true )] out string errorMessage )
        => throw new NotSupportedException( "Unattended license source doesn't support license registration." );

    bool ILicense.TryGetConsumptionProperties(
        LicenseConsumptionOptions options,
        [MaybeNullWhen( false )] out LicenseConsumptionProperties licenseConsumptionProperties,
        [MaybeNullWhen( true )] out string errorMessage )
    {
        licenseConsumptionProperties = new LicenseConsumptionProperties(
            LicenseProduct.MetalamaProfessional,
            LicenseType.Unattended,
            null,
            "Unattended Process License",
            new Version( 0, 0 ),
            null,
            false,
            false,
            null,
            SubscriptionStatus.None,
            LicenseGeneration.Current,
            ServicingPhase.LongTerm );

        errorMessage = null;

        return true;
    }

    bool ILicense.TryGetRegistrationProperties(
        [MaybeNullWhen( false )] out LicenseRegistrationProperties licenseProperties,
        [MaybeNullWhen( true )] out string errorMessage )
        => throw new NotSupportedException( "Unattended license source doesn't support license registration." );

    public void OnConsumed() { }

    event Action? ILicenseSource.Changed { add { } remove { } }

    public LicenseSourcePriority Priority => LicenseSourcePriority.Unattended;
}