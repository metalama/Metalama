// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
    /// Gets the minimal versions of the registered license keys that the running version of Metalama cannot consume,
    /// ordered by version. Such a license key is stored in a group named after the version, and the version is the
    /// only information about it that the running version has, because the license key itself is not deserialized.
    /// </summary>
    IEnumerable<Version> UnsupportedRegisteredLicenseVersions { get; }

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