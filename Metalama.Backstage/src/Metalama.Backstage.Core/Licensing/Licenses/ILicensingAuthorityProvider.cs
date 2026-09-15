// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using System.Collections.Generic;

namespace Metalama.Backstage.Licensing.Licenses;

/// <summary>
/// Provides the licensing authority of a key, which signs a license key or verifies its signature.
/// </summary>
public interface ILicensingAuthorityProvider : IBackstageService
{
    /// <summary>
    /// Gets the identifiers of the keys of the current provider.
    /// </summary>
    IEnumerable<byte> KeyIds { get; }

    /// <summary>
    /// Gets the authority of a key. The authority is created when it is required for the first time.
    /// </summary>
    /// <param name="keyId">The identifier of the key.</param>
    /// <returns>The authority of the key of identifier <paramref name="keyId"/>.</returns>
    /// <exception cref="KeyNotFoundException">The current provider has no key of identifier <paramref name="keyId"/>.</exception>
    LicensingAuthority GetAuthority( byte keyId );
}
