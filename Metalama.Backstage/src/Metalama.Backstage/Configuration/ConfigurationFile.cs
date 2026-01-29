// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Text.Json.Serialization;

namespace Metalama.Backstage.Configuration;

public abstract record ConfigurationFile
{
    private DateTime? _fileSystemTimestamp;

    /// <summary>
    /// Gets a distinct timestamp for the current object.
    /// </summary>
    [JsonIgnore]
    internal ConfigurationFileTimestamp? Timestamp
        => this._fileSystemTimestamp == null ? null : new ConfigurationFileTimestamp( this._fileSystemTimestamp.Value, this.Version );

    internal void SetFileSystemTimestamp( DateTime value )
    {
        this._fileSystemTimestamp = value.ToUniversalTime();
    }

    internal void IncrementVersion()
    {
        this.Version = (this.Version ?? 0) + 1;
    }

    /// <summary>
    /// Gets or sets a version number of this object.  We don't expect the user (or other versions of Metalama.Backstage) to change this property.
    /// Its value is only taken into account when comparing two objects with the same filesystem timestamp.
    /// </summary>
    [JsonPropertyName( "version" )]
    public int? Version { get; set; }

    public virtual void Validate( Action<string> reportWarning ) { }
}
