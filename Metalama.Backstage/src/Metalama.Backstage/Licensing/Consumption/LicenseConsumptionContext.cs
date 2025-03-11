// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using System;

namespace Metalama.Backstage.Licensing.Consumption;

public sealed class LicenseConsumptionContext
{
    public LicenseConsumptionContext( LicenseConsumptionProperties license, IApplicationInfo applicationInfo, DateTime date, ILogger logger )
    {
        this.License = license;
        this.ApplicationInfo = applicationInfo;
        this.Date = date;
        this.Logger = logger;
    }

    public LicenseConsumptionProperties License { get; init; }

    public IApplicationInfo ApplicationInfo { get; init; }

    public DateTime Date { get; init; }

    public ILogger Logger { get; init; }
}