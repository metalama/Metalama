// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System.Collections.Generic;
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

    void RemoveLicenses();

    public IEnumerable<LicenseRegistrationProperties> RegisteredLicenses { get; }

    /// <summary>
    /// Validates the license key and returns a value indicating whether it can be registered using <see cref="TryRegisterLicense"/>.
    /// </summary>
    bool TryValidateLicenseKey( string licenseKey, [NotNullWhen( false )] out string? errorMessage );

    /// <summary>
    /// Attempts to parse a license key into a <see cref="LicenseRegistrationProperties"/>, but does not test
    /// whether this license key can be registered.
    /// </summary>
    bool TryParseLicenseKey(
        string licenseKey,
        [NotNullWhen( false )] out string? errorMessage,
        [NotNullWhen( true )] out LicenseRegistrationProperties? licenseProperties );
}