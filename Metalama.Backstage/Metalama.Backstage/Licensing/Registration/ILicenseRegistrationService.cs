// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Licensing.Registration;

[PublicAPI]
public interface ILicenseRegistrationService : IBackstageService, INotifyPropertyChanged
{
    bool TryRegisterCommunityEdition( [NotNullWhen( false )] out string? errorMessage );

    bool TryRegisterTrialEdition( [NotNullWhen( false )] out string? errorMessage );

    bool TryRegisterLicense( string licenseString, [NotNullWhen( false )] out string? errorMessage );

    bool CanRegisterTrialEdition { get; }

    bool TryRemoveCurrentLicense( [NotNullWhen( true )] out string? licenseString );

    LicenseProperties? RegisteredLicense { get; }

    bool TryValidateLicenseKey( string licenseKey, [NotNullWhen( false )] out string? errorMessage );

    bool TryParseLicenseKey(
        string licenseKey,
        [NotNullWhen( false )] out string? errorMessage,
        [NotNullWhen( true )] out LicenseProperties? licenseProperties );
}