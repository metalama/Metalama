// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using System;

namespace Metalama.Backstage.Licensing.Consumption;

[PublicAPI]
public sealed class LicenseConsumptionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LicenseConsumptionContext"/> class.
    /// </summary>
    public LicenseConsumptionContext(
        LicenseConsumptionProperties license,
        IApplicationInfo applicationInfo,
        ProductProfile productProfile,
        DateTime date,
        ILogger logger )
    {
        this.License = license;
        this.ApplicationInfo = applicationInfo;
        this.ProductProfile = productProfile;
        this.Date = date;
        this.Logger = logger;
    }

    /// <summary>
    /// Gets the profile of the product family that consumes the license.
    /// </summary>
    public ProductProfile ProductProfile { get; init; }

    public LicenseConsumptionProperties License { get; init; }

    public IApplicationInfo ApplicationInfo { get; init; }

    public DateTime Date { get; init; }

    public ILogger Logger { get; init; }
}