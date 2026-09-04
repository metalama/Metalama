// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metalama.Backstage.Configuration;

[PublicAPI]
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

    /// <summary>
    /// Gets or sets the members of the configuration file that this version of Metalama does not declare.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Several versions of Metalama share the same configuration files of the user profile, and a configuration file
    /// is rewritten from the record that represents it. A version that did not store the members it does not declare
    /// would therefore remove the content written by a newer version. The unmapped members are read into this
    /// property and written back unchanged.
    /// </para>
    /// <para>
    /// A type that is nested in a configuration file does not derive from <see cref="ConfigurationFile"/> and
    /// declares its own property with the same purpose, because the extension data of an object is carried by the
    /// object itself.
    /// </para>
    /// <para>
    /// The property has a setter and not an initializer, because the source generator of <c>System.Text.Json</c>
    /// maps a property declared with an initializer to a parameter of the deserialization constructor, and a
    /// parameter cannot receive the extension data.
    /// </para>
    /// </remarks>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? UnknownMembers { get; set; }

    public virtual void Validate( Action<string> reportWarning ) { }
}