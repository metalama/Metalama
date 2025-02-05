// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Consumption;

namespace Metalama.Backstage.Licensing.Audit;

internal interface ILicenseAuditManager : IBackstageService
{
    void ReportLicense( LicenseConsumptionData license );
}