// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Newtonsoft.Json;
using System;
using System.IO;

namespace Metalama.Backstage.Configuration;

public abstract record ConfigurationFile
{
    private DateTime? _fileSystemTimestamp;

    /// <summary>
    /// Gets a distinct timestamp for the current object.
    /// </summary>
    internal ConfigurationFileTimestamp? Timestamp
        => this._fileSystemTimestamp == null ? null : new ConfigurationFileTimestamp( this._fileSystemTimestamp.Value, this.Version );

    internal void SetFilesystemTimestamp( DateTime value )
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
    [JsonProperty( "version" )]
    public int? Version { get; set; }

    public string ToJson()
    {
        // Serialize.
        var textWriter = new StringWriter();
        var serializer = JsonSerializer.Create();
        serializer.Formatting = Formatting.Indented;
        serializer.Serialize( textWriter, this );

        return textWriter.ToString();
    }

    internal bool StructurallyEqualsTo( ConfigurationFile other )
        => (this with { Version = null }).ToJson().Equals( (other with { Version = null }).ToJson(), StringComparison.Ordinal );

    public virtual void Validate( Action<string> reportWarning ) { }
}