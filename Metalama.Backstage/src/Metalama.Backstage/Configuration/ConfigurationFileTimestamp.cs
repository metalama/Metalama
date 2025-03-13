// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Globalization;

namespace Metalama.Backstage.Configuration;

public readonly struct ConfigurationFileTimestamp : IEquatable<ConfigurationFileTimestamp>
{
    private readonly DateTime _lastModificationTime;
    private readonly int _version;

    internal ConfigurationFileTimestamp( DateTime lastModificationTime, int? version )
    {
        if ( lastModificationTime.Kind != DateTimeKind.Utc )
        {
            throw new ArgumentOutOfRangeException( nameof(lastModificationTime), "A UTC timestamp was expected." );
        }

        this._lastModificationTime = lastModificationTime;
        this._version = version ?? 0;
    }

    public DateTime ToUtcDateTime() => this._lastModificationTime;

    public override string ToString() => this._lastModificationTime.ToString( CultureInfo.InvariantCulture ) + ":" + this._version;

    public bool Equals( ConfigurationFileTimestamp other ) => this._lastModificationTime == other._lastModificationTime && this._version == other._version;

    public override bool Equals( object? obj ) => obj is ConfigurationFileTimestamp other && this.Equals( other );

    public override int GetHashCode() => HashCode.Combine( this._lastModificationTime, this._version );

    public static bool operator ==( ConfigurationFileTimestamp left, ConfigurationFileTimestamp right ) => left.Equals( right );

    public static bool operator !=( ConfigurationFileTimestamp left, ConfigurationFileTimestamp right ) => !left.Equals( right );

    public bool IsOlderThan( in ConfigurationFileTimestamp other )
    {
        if ( this._lastModificationTime == other._lastModificationTime )
        {
            return this._version < other._version;
        }
        else if ( this._lastModificationTime < other._lastModificationTime )
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}