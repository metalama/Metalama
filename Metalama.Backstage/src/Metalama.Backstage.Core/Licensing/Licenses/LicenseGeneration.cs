// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Licensing.Licenses;

public enum LicenseGeneration : byte
{
    /// <summary>
    /// Licenses before generations were introduced.
    /// </summary>
    None = 0,

    /// <summary>
    /// License generation defined in 2025-Q2 (version 2025.1 onward).
    /// </summary>
    V2025Q2 = 1,

    Current = V2025Q2
}