// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metalama.Backstage.Tests.ConfigurationManager;

[ConfigurationFile( "test.json" )]
internal sealed record TestConfigurationFile : ConfigurationFile
{
    public bool IsModified { get; init; }

    /// <summary>
    /// Gets an accumulating record of what each update contributed, to which every writer appends.
    /// </summary>
    /// <remarks>
    /// A test that only counted the successful updates could not tell an update that was lost from one that was
    /// declined, because both leave the count of successes right. Accumulating instead makes a lost update visible
    /// as a missing contribution. It is a string rather than a collection so that the structural equality of the
    /// record, which decides whether an update changes anything, compares the contents and not the reference.
    /// </remarks>
    public string Marks { get; init; } = "";
}

/// <summary>
/// A second configuration file, stored separately from <see cref="TestConfigurationFile"/>.
/// </summary>
/// <remarks>
/// It exists so that a test can distinguish operations that are serialized because they concern the same file from
/// operations that are serialized because the implementation locks more than the file it is about.
/// </remarks>
[ConfigurationFile( "test2.json" )]
internal sealed record SecondTestConfigurationFile : ConfigurationFile
{
    public bool IsModified { get; init; }
}

/// <summary>
/// A configuration file whose members are nested objects, so that a test can observe what
/// <see cref="Configuration.ConfigurationManager"/> does with a member that the running version does not declare at
/// every level of a document.
/// </summary>
[ConfigurationFile( "test-nested.json" )]
internal sealed record NestedTestConfigurationFile : ConfigurationFile
{
    /// <summary>
    /// Gets the number of updates that were applied, which gives a test a known member to change.
    /// </summary>
    public int Counter { get; init; }

    /// <summary>
    /// Gets a nested object, which stands for a member such as <c>DiagnosticsConfiguration.Logging</c>.
    /// </summary>
    public TestNestedObject Nested { get; init; } = new();

    /// <summary>
    /// Gets a dictionary whose values are objects, which stands for a member such as
    /// <c>ToastNotificationsConfiguration.Notifications</c>.
    /// </summary>
    public ImmutableDictionary<string, TestNestedObject> Map { get; init; } = ImmutableDictionary<string, TestNestedObject>.Empty;

    /// <summary>
    /// Gets an array whose elements are objects, so that a test covers the level of nesting that a merge of two JSON
    /// documents could not handle.
    /// </summary>
    public ImmutableArray<TestNestedObject> Items { get; init; } = ImmutableArray<TestNestedObject>.Empty;
}

/// <summary>
/// An object nested in <see cref="NestedTestConfigurationFile"/>.
/// </summary>
/// <remarks>
/// The type does not derive from <see cref="ConfigurationFile"/>, so it declares its own extension data, exactly as
/// the nested types of the product do.
/// </remarks>
internal sealed record TestNestedObject
{
    public bool IsModified { get; init; }

    /// <summary>
    /// Gets or sets the members that the running version does not declare.
    /// </summary>
    /// <remarks>
    /// The property has a setter and not an initializer, because the source generator of
    /// <c>System.Text.Json</c> maps an initializer to a parameter of the deserialization constructor, and a
    /// parameter cannot receive the extension data.
    /// </remarks>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? UnknownMembers { get; set; }
}
