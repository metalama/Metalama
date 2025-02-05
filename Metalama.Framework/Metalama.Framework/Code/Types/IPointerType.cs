// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Code.Types
{
    /// <summary>
    /// Represents an unsafe pointer type.
    /// </summary>
    public interface IPointerType : IType
    {
        /// <summary>
        /// Gets the type pointed at, that is, <c>T</c> for <c>T*</c>.
        /// </summary>
        IType PointedAtType { get; }
    }
}