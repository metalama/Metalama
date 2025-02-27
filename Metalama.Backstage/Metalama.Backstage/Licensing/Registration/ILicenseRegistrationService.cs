// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Metalama.Backstage.Licensing.Registration;

[PublicAPI]
public interface ILicenseRegistrationService : IBackstageService, INotifyPropertyChanged
{
    LicenseRegistrationResult RegisterCommunityEdition( CommunityLicenseReason reason );

    [Obsolete]
    LicenseRegistrationResult RegisterLegacyFreeEdition();

    LicenseRegistrationResult RegisterTrialEdition();

    LicenseRegistrationResult RegisterLicense( string licenseString );

    bool CanRegisterTrialEdition { get; }

    void RemoveLicenses();

    IEnumerable<LicenseRegistrationProperties> RegisteredLicenses { get; }

    /// <summary>
    /// Validates the license key and returns a value indicating whether it can be registered using <see cref="RegisterLicense"/>.
    /// </summary>
    LicenseRegistrationResult ValidateLicenseKey( string licenseKey );

    /// <summary>
    /// Attempts to parse a license key into a <see cref="LicenseRegistrationProperties"/>, but does not test
    /// whether this license key can be registered.
    /// </summary>
    LicenseRegistrationResult ParseLicenseKey( string licenseKey );
}