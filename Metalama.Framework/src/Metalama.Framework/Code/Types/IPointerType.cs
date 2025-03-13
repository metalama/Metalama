// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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