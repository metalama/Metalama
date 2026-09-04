// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metalama.Backstage.Configuration;

/// <summary>
/// The base record of every object that is serialized into a configuration file, that is, of the root object of the
/// file, which derives from <see cref="ConfigurationFile"/>, and of every object nested in it.
/// </summary>
[PublicAPI]
public abstract record ConfigurationObject
{
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
    /// The property has a setter and not an initializer, because the source generator of <c>System.Text.Json</c>
    /// maps a property declared with an initializer to a parameter of the deserialization constructor, and a
    /// parameter cannot receive the extension data.
    /// </para>
    /// </remarks>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? UnknownMembers { get; set; }

    /// <summary>
    /// Copies into the current object the members that the object it replaces in a configuration file carried and
    /// that this version of Metalama does not declare.
    /// </summary>
    /// <remarks>
    /// A transformation written as <c>value with { … }</c> carries <see cref="UnknownMembers"/> over by itself.
    /// A transformation that builds a new instance instead, as the update of an object nested in a dictionary does,
    /// has to call this method with the value that the new instance replaces. Otherwise the members written by a
    /// newer version of Metalama are removed from the file.
    /// </remarks>
    /// <param name="previousValue">The object that the current object replaces, or <c>null</c> if there is none.</param>
    public void CopyUnknownMembersFrom( ConfigurationObject? previousValue ) => this.UnknownMembers = previousValue?.UnknownMembers;
}
