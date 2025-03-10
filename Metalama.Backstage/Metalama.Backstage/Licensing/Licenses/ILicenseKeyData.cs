// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Licenses.LicenseFields;
using System.Collections.Generic;

namespace Metalama.Backstage.Licensing.Licenses;

internal interface ILicenseKeyData
{
    byte Version { get; }

    int LicenseId { get; }

    LicenseType LicenseType { get; }

    LicenseProduct Product { get; }

    IReadOnlyDictionary<LicenseFieldIndex, LicenseField> Fields { get; }
}