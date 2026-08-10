// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;

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
