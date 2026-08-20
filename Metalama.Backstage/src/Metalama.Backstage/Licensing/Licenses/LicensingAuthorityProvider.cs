// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Backstage.Licensing.Licenses;

/// <summary>
/// Base implementation of <see cref="ILicensingAuthorityProvider"/>. It creates the authority of a key, and therefore
/// the <see cref="System.Security.Cryptography.DSA"/> object of that key, when the authority is required for the first
/// time.
/// </summary>
/// <remarks>
/// The authority is not created while the licensing services are registered, because an unsigned license key requires
/// no signature verification and therefore no authority at all. Finite field DSA is unavailable on macOS since
/// .NET 11, where the creation of a <see cref="System.Security.Cryptography.DSA"/> object throws
/// <see cref="PlatformNotSupportedException"/>.
/// </remarks>
public abstract class LicensingAuthorityProvider : ILicensingAuthorityProvider
{
    private readonly Dictionary<byte, Lazy<LicensingAuthority>> _authorities;

    /// <summary>
    /// Initializes a new instance of the <see cref="LicensingAuthorityProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider that provides the observer of the new provider, or <c>null</c> if the new provider has no observer.</param>
    /// <param name="keyIds">The identifiers of the keys of the new provider.</param>
    protected LicensingAuthorityProvider( IServiceProvider? serviceProvider, IEnumerable<byte> keyIds )
    {
        var observer = serviceProvider?.GetBackstageService<ILicensingAuthorityObserver>();

        this._authorities = keyIds.ToDictionary(
            keyId => keyId,
            keyId => new Lazy<LicensingAuthority>(
                () =>
                {
                    observer?.OnLicensingAuthorityCreating( keyId );

                    return this.CreateAuthority( keyId );
                } ) );
    }

    /// <inheritdoc />
    public IEnumerable<byte> KeyIds => this._authorities.Keys;

    /// <inheritdoc />
    public LicensingAuthority GetAuthority( byte keyId )
    {
        if ( !this._authorities.TryGetValue( keyId, out var authority ) )
        {
            throw new KeyNotFoundException( $"There is no licensing authority key of identifier {keyId}." );
        }

        return authority.Value;
    }

    /// <summary>
    /// Creates the authority of a key.
    /// </summary>
    /// <param name="keyId">The identifier of the key, which is one of <see cref="KeyIds"/>.</param>
    /// <returns>The authority of the key of identifier <paramref name="keyId"/>.</returns>
    protected abstract LicensingAuthority CreateAuthority( byte keyId );
}
