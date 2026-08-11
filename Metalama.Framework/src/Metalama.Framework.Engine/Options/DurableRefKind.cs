// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Options;

/// <summary>
/// Identifies the kind of durable reference that the project produces. Settable via the
/// <c>MetalamaDurableRefKind</c> MSBuild property and exposed as <see cref="IProjectOptions.DurableRefKind"/>.
/// </summary>
/// <remarks>
/// <para>
/// A durable reference allows an object that outlives a single request to hold a reference to a declaration without
/// keeping a compilation in memory. This requirement applies to design time, where the analysis process is long-lived
/// and Roslyn creates a new compilation after each modification of the source code. A batch compilation processes a
/// single compilation, which lives until the build ends, so the conversion to an identifier and the resolution of that
/// identifier are unnecessary in that scenario.
/// </para>
/// <para>
/// The default value therefore depends on the execution scenario. The other values override that selection, mainly so
/// that the tests can exercise a representation that their own execution scenario would not select.
/// </para>
/// </remarks>
public enum DurableRefKind
{
    /// <summary>
    /// The representation is selected by the execution scenario: <see cref="Bound"/> during a batch compilation, and
    /// <see cref="Serializable"/> in every other scenario.
    /// </summary>
    Default = 0,

    /// <summary>
    /// A durable reference stores the reference it was created from, and computes its identifier only when the
    /// identifier is requested, for instance during serialization.
    /// </summary>
    Bound = 1,

    /// <summary>
    /// A durable reference stores only its identifier. It also caches the reference returned by its last resolution,
    /// through a weak reference, so that a second resolution in the same compilation does not resolve the identifier
    /// through the symbol table again.
    /// </summary>
    Serializable = 2,

    /// <summary>
    /// The same representation as <see cref="Serializable"/>, without the cache, so that every resolution goes through
    /// the symbol table.
    /// </summary>
    /// <remarks>
    /// This value is used by the test suites. It keeps the identifier resolution code covered by tests whose results
    /// would otherwise come from the cache.
    /// </remarks>
    SerializableWithoutCache = 3
}
