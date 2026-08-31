// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Options;
using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// The source of the fragment that both test classes below analyze. It is the shape that an options class of a
/// consuming repository has: a class implementing <c>IHierarchicalOptions</c>, whose properties are the two
/// incremental collections of the framework, and a keyed-collection item.
/// </summary>
/// <remarks>
/// No type of the fragment carries a contract attribute. Both contracts reach every type of it from
/// <see cref="IIncrementalObject"/>, so an attribute here would be redundant and reported as such.
/// </remarks>
internal static class OptionsCollectionCode
{
    public const string Options = """
                                  using Metalama.Framework.Options;

                                  class Library : IIncrementalKeyedCollectionItem<string>
                                  {
                                      public Library( string name ) { this.Name = name; }

                                      public string Name { get; }

                                      string IIncrementalKeyedCollectionItem<string>.Key => this.Name;

                                      public object ApplyChanges( object changes, in ApplyChangesContext context ) => changes;
                                  }

                                  class MyOptions : IHierarchicalOptions
                                  {
                                      public IncrementalHashSet<string> Included { get; init; } = IncrementalHashSet.Empty<string>();

                                      public IncrementalKeyedCollection<string, Library> Libraries { get; init; }
                                          = IncrementalKeyedCollection.Empty<string, Library>();

                                      public object ApplyChanges( object changes, in ApplyChangesContext context ) => this;
                                  }
                                  """;
}

/// <summary>
/// Tests that an options class storing the incremental collections of the framework satisfies the durable contract
/// that <see cref="IIncrementalObject"/> imposes on it.
/// </summary>
/// <remarks>
/// <see cref="IncrementalHashSet{T}"/> reached no contract before the marker moved to
/// <see cref="IIncrementalObject"/>, so every options class that had a property of that type was reported. No type
/// of this repository has such a property, which is why the omission was found by a consuming repository.
/// </remarks>
public sealed class DurableOptionsCollectionTests : DurableAnalyzerTestBase
{
    [Fact]
    public async Task IncrementalCollectionsOfAnOptionsClass_AreNotReported()
        => await AssertNoDiagnosticAsync( OptionsCollectionCode.Options );
}

/// <summary>
/// Tests that an options class storing the incremental collections of the framework satisfies the immutable contract
/// that <see cref="IIncrementalObject"/> imposes on it.
/// </summary>
/// <remarks>
/// Both collections are trusted for the type arguments they store, so the verdict of a property is that of its type
/// arguments.
/// </remarks>
public sealed class ImmutableOptionsCollectionTests : ImmutableAnalyzerTestBase
{
    [Fact]
    public async Task IncrementalCollectionsOfAnOptionsClass_AreNotReported()
        => await AssertNoDiagnosticAsync( OptionsCollectionCode.Options );
}
