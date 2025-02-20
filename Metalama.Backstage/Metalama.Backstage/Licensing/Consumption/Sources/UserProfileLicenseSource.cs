// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Licensing.Registration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Backstage.Licensing.Consumption.Sources;

/// <summary>
/// License source providing licenses from a license file.
/// </summary>
internal sealed class UserProfileLicenseSource : LicenseSourceBase
{
    private LicensingConfiguration _licensingConfiguration;

    public override string Description => "user profile";

    public override LicenseSourceKind Kind => LicenseSourceKind.UserProfile;

    protected override IEnumerable<LicenseRegistrationProperties> GetRegisteredLicenses( Action<LicensingMessage> reportMessage )
    {
        return this._licensingConfiguration.GetRegisteredLicenses( reportMessage )
            .Select( l => l.ToLicenseRegistrationProperties() );
    }

    public UserProfileLicenseSource( IServiceProvider services )
        : base( services )
    {
        var configurationManager = services.GetRequiredBackstageService<IConfigurationManager>();
        this._licensingConfiguration = configurationManager.Get<LicensingConfiguration>();
        configurationManager.ConfigurationFileChanged += this.OnConfigurationFileChanged;
    }

    private void OnConfigurationFileChanged( ConfigurationFile file )
    {
        if ( file is LicensingConfiguration licensingConfiguration )
        {
            this._licensingConfiguration = licensingConfiguration;
            this.OnChanged();
        }
    }

    public override LicenseSourcePriority Priority => LicenseSourcePriority.UserProfile;
}