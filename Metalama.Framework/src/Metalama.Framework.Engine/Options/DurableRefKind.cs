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
/// A durable reference exists so that an object outliving a single request does not keep a compilation in memory,
/// which is a design-time requirement: the analysis process is long-lived and Roslyn produces a new compilation on
/// every keystroke. A batch compilation has one compilation that outlives everything the run produces, so the
/// identifier round trip buys nothing there.
/// </para>
/// <para>
/// The default is therefore scope-dependent, and the other values exist to override that choice, mostly so that tests
/// can exercise a kind that the scope would not select on its own.
/// </para>
/// </remarks>
public enum DurableRefKind
{
    /// <summary>
    /// The kind is chosen by the execution scenario: <see cref="Live"/> for a batch compilation and
    /// <see cref="Serializable"/> everywhere else.
    /// </summary>
    Default = 0,

    /// <summary>
    /// A durable reference holds the reference it was made from, and computes its identifier only when one is asked
    /// for, such as when it is serialized.
    /// </summary>
    Live = 1,

    /// <summary>
    /// A durable reference holds only its identifier, and caches the reference it last resolved to through a weak
    /// reference, so that a repeated resolution against a live compilation does not walk the symbol table again.
    /// </summary>
    Serializable = 2,

    /// <summary>
    /// As <see cref="Serializable"/>, but without the cache, so that every resolution goes through the symbol table.
    /// </summary>
    /// <remarks>
    /// This value exists for the test suites: it keeps the identifier resolution path covered by tests that would
    /// otherwise answer from the cache.
    /// </remarks>
    SerializableWithoutCache = 3
}
