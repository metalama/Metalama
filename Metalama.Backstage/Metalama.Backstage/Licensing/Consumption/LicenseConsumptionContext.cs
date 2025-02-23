// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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