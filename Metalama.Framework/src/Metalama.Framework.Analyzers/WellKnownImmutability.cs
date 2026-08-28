// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Analyzers.Immutability
{
    /// <summary>
    /// How a well-known type is classified, without examining its members.
    /// </summary>
    internal enum WellKnownImmutability
    {
        /// <summary>
        /// Immutable whatever its type arguments are, which are not examined at all.
        /// </summary>
        Immutable,

        /// <summary>
        /// Never immutable.
        /// </summary>
        NotImmutable,

        /// <summary>
        /// Immutable exactly when its type arguments are immutable: all of them by default, or, when the entry carries
        /// an argument mask, the ones the mask selects. <c>ImmutableArray&lt;T&gt;</c> is transparent in <c>T</c>, and
        /// <c>ImmutableDictionary&lt;TKey,TValue&gt;</c> in both.
        /// </summary>
        /// <remarks>
        /// This is what makes the contract deep. <c>Metalama.Patterns.Immutability</c> reaches the same result by a
        /// different route: its <c>ImmutableCollectionClassifier</c> recurses one level per call and demotes the
        /// collection to <c>Shallow</c> when an argument is not <c>Deep</c>, but since it calls the classifier again
        /// on each argument, the composition is fully recursive. Here the recursion is direct, and there is no
        /// shallow kind at all: anything short of immutable is reported.
        /// </remarks>
        Transparent
    }
}
