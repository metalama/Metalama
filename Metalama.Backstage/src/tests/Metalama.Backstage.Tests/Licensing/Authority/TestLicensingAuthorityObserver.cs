// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Licenses;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Backstage.Tests.Licensing.Authority;

/// <summary>
/// Records the identifier of every key whose authority a <see cref="LicensingAuthorityProvider"/> creates.
/// </summary>
internal sealed class TestLicensingAuthorityObserver : ILicensingAuthorityObserver
{
    private readonly object _sync = new();
    private ImmutableArray<byte> _createdAuthorityKeyIds = ImmutableArray<byte>.Empty;

    /// <summary>
    /// Gets the identifiers of the keys whose authority has been created, in the order of their creation.
    /// </summary>
    public IReadOnlyList<byte> CreatedAuthorityKeyIds
    {
        get
        {
            lock ( this._sync )
            {
                return this._createdAuthorityKeyIds;
            }
        }
    }

    /// <inheritdoc />
    public void OnLicensingAuthorityCreating( byte keyId )
    {
        lock ( this._sync )
        {
            this._createdAuthorityKeyIds = this._createdAuthorityKeyIds.Add( keyId );
        }
    }
}
