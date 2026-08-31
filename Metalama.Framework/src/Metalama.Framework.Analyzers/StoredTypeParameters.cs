// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Analyzers
{
    /// <summary>
    /// The set of type parameters of a generic definition that are stored in one of its instance fields, addressed by
    /// ordinal.
    /// </summary>
    /// <remarks>
    /// A bit set in a <see cref="ulong"/>, which is what a set of at most sixty-four small ordinals should be, wrapped
    /// so that the callers say what they mean. A bare <c>ulong</c> travelled through three signatures and two callers
    /// with nothing to say what it was, and every caller had to repeat the shift.
    /// </remarks>
    internal readonly struct StoredTypeParameters
    {
        /// <summary>
        /// The number of ordinals a <see cref="ulong"/> can address. A definition with more type parameters than this
        /// does not exist in practice; the computation treats every parameter beyond it as stored, which is the
        /// conservative answer.
        /// </summary>
        public const int MaxOrdinal = 64;

        private readonly ulong _bits;

        public StoredTypeParameters( ulong bits )
        {
            this._bits = bits;
        }

        /// <summary>
        /// Determines whether the type parameter at an ordinal is stored, and therefore whether the type argument
        /// supplied for it at a construction site has to satisfy the contract.
        /// </summary>
        /// <remarks>
        /// An ordinal at or beyond <see cref="MaxOrdinal"/> is reported as stored, which is conservative: it means the
        /// argument is checked rather than trusted.
        /// </remarks>
        public bool IsStored( int ordinal )
            => ordinal >= MaxOrdinal || (this._bits & (1UL << ordinal)) != 0;
    }
}
