// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Analyzers
{
    /// <summary>
    /// How a well-known type is classified, without examining its members.
    /// </summary>
    internal enum WellKnownDurability
    {
        /// <summary>
        /// Durable whatever its type arguments are, which are not examined at all. Also used for the types at which
        /// the walk stops because a chain through them explains nothing, which mirrors
        /// <c>UserCodeRetentionPolicy.IsBoundary</c>.
        /// </summary>
        Durable,

        /// <summary>
        /// Never durable.
        /// </summary>
        NotDurable,

        /// <summary>
        /// Durable exactly when its type arguments are durable: all of them by default, or, when the entry carries an
        /// argument mask, the ones the mask selects. <c>List&lt;T&gt;</c> is transparent in <c>T</c>, and
        /// <c>Dictionary&lt;TKey,TValue&gt;</c> in both, whereas <c>ConditionalWeakTable&lt;TKey,TValue&gt;</c> carries
        /// a mask because the table does not keep its key alive and only the value is examined.
        /// </summary>
        Transparent
    }
}
