// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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