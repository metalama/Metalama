// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Backstage.Licensing.Licenses;

/// <summary>
/// Provides the authorities of a set of keys given in their XML representation.
/// </summary>
/// <remarks>
/// The license key generator uses this provider to verify the signature that it has just created, with the same
/// private key as the one it signed with.
/// </remarks>
[PublicAPI( "Use in the license generator web and API." )]
public sealed class ExplicitLicensingAuthorityProvider : LicensingAuthorityProvider
{
    private readonly IReadOnlyDictionary<byte, string> _keys;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExplicitLicensingAuthorityProvider"/> class.
    /// </summary>
    /// <param name="keys">The identifier and the XML representation of each key of the new provider.</param>
    public ExplicitLicensingAuthorityProvider( params IEnumerable<(int Id, string Key)> keys )
        : this( keys.ToDictionary( x => checked((byte) x.Id), x => x.Key ) ) { }

    private ExplicitLicensingAuthorityProvider( IReadOnlyDictionary<byte, string> keys ) : base( null, keys.Keys )
    {
        this._keys = keys;
    }

    /// <inheritdoc />
    protected override LicensingAuthority CreateAuthority( byte keyId ) => new( keyId, this._keys[keyId] );
}
