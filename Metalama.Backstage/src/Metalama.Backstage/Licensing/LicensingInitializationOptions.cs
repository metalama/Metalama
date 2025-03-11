// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Licensing.Licenses;
using System;

namespace Metalama.Backstage.Licensing;

[PublicAPI]
public record LicensingInitializationOptions
{
    public static LicensingInitializationOptions Default { get; } = new LicensingInitializationOptions();

    public static LicensingInitializationOptions ForTest( Action<LicenseKeyDataBuilder> buildTestLicenseAction )
        => new LicensingInitializationOptions
        {
            UseTestAuthority = true, IgnoredLicenseSources = LicenseSourceKind.All, BuildTestLicenseAction = buildTestLicenseAction
        };

    internal LicenseSourceKind IgnoredLicenseSources { get; init; } = LicenseSourceKind.None;

    /// <summary>
    /// Gets a value indicating whether the test licensing authority should be used.
    /// This property may be set to <c>true</c> only for unit tests.
    /// </summary>
    internal bool UseTestAuthority { get; init; }
    
    /// <summary>
    /// Gets a delegate that can configure a <see cref="LicenseKeyDataBuilder"/>.
    /// This property is only taken into account when <see cref="UseTestAuthority"/> is <c>true</c>.
    /// In this case, profile licenses are not loaded.
    /// </summary>
    internal Action<LicenseKeyDataBuilder>? BuildTestLicenseAction { get; init; }
}