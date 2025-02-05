// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Licenses.LicenseFields;
using System.Collections.Generic;

namespace Metalama.Backstage.Licensing.Licenses;

internal interface ILicenseKeyData
{
    byte Version { get; }

    int LicenseId { get; }

    LicenseType LicenseType { get; }

    LicensedProduct Product { get; }

    IReadOnlyDictionary<LicenseFieldIndex, LicenseField> Fields { get; }
}