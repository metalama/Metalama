// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Backstage.Licensing.Consumption;

[PublicAPI]
public record LicensingInitializationOptions
{
    public bool IgnoreUnattendedProcessLicense { get; init; }

    public bool IgnoreUserProfileLicenses { get; init; }
}