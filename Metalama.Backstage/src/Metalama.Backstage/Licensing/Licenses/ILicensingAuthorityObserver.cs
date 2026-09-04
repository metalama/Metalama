// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.Licensing.Licenses;

/// <summary>
/// Observes the creation of the licensing authorities by a <see cref="LicensingAuthorityProvider"/>. A test registers
/// an implementation of this interface to assert that a code path creates no authority, and therefore no cryptographic
/// object, in particular no <see cref="System.Security.Cryptography.DSA"/> object.
/// </summary>
/// <remarks>
/// This observation cannot be replaced by running the code path on a platform where finite field DSA is unavailable,
/// because the test suite does not run on such a platform.
/// </remarks>
internal interface ILicensingAuthorityObserver : IBackstageService
{
    /// <summary>
    /// Method called before the provider creates the authority of a key.
    /// </summary>
    /// <param name="keyId">The identifier of the key.</param>
    /// <remarks>
    /// The method is called before the creation and not after it, so that a test observes the attempt even on a
    /// platform where the creation throws.
    /// </remarks>
    void OnLicensingAuthorityCreating( byte keyId );
}
