// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Options;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating.Immutability;

/// <summary>
/// Tests that an options class storing the incremental collections of the framework satisfies the immutable contract
/// that <see cref="IIncrementalObject"/> imposes on it.
/// </summary>
/// <remarks>
/// Both collections are trusted for the type arguments they store, so the verdict of a property is that of its type
/// arguments.
/// </remarks>
public sealed class ImmutableOptionsCollectionTests : ImmutabilityTestBase
{
    private const string _options = """
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

    [Fact]
    public async Task IncrementalCollectionsOfAnOptionsClass_AreNotReported() => await AssertNoDiagnosticAsync( _options );
}
