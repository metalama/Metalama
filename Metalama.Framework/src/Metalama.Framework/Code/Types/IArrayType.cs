// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code.Types
{
    /// <summary>
    /// Represents an array, e.g. <c>T[]</c>.
    /// </summary>
    public interface IArrayType : IType
    {
        /// <summary>
        /// Gets the element type, e.g. the <c>T</c> in <c>T[]</c>.
        /// </summary>
        IType ElementType { get; }

        /// <summary>
        /// Gets the array rank (1 for <c>T[]</c>, 2 for <c>T[,]</c>, ...).
        /// </summary>
        int Rank { get; }

        new IArrayType ToNullable();

        new IArrayType ToNonNullable();
    }
}