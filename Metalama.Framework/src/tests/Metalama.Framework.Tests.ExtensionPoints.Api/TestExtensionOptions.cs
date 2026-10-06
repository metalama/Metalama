// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Options;

namespace Metalama.Framework.Tests.ExtensionPoints;

/// <summary>
/// Hierarchical options declared by the API of the test extension, which is an additional compile-time assembly and not a compile-time
/// project. The test <c>Options/Options_FromCompileTimeAssembly</c> verifies that the engine registers them.
/// </summary>
[HierarchicalOptions( InheritedByDerivedTypes = false, InheritedByOverridingMembers = false )]
public sealed class TestExtensionOptions : IHierarchicalOptions<ICompilation>, IHierarchicalOptions<INamespace>, IHierarchicalOptions<INamedType>,
                                           IHierarchicalOptions<IMethod>
{
    /// <summary>
    /// Gets a value that the test sets and reads, or <c>null</c> when the value is inherited.
    /// </summary>
    public string? Value { get; init; }

    /// <inheritdoc />
    object IIncrementalObject.ApplyChanges( object changes, in ApplyChangesContext context )
        => new TestExtensionOptions { Value = ((TestExtensionOptions) changes).Value ?? this.Value };

    /// <inheritdoc />
    IHierarchicalOptions IHierarchicalOptions.GetDefaultOptions( OptionsInitializationContext context ) => new TestExtensionOptions { Value = "default" };
}
