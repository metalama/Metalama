// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Exposes a <see cref="Type"/> property.
    /// </summary>
    [CompileTime]
    public interface IHasType
    {
        /// <summary>
        /// Gets the type of the expression, member, or parameter.
        /// </summary>
        IType Type { get; }

        /// <summary>
        /// Gets the <see cref="Code.RefKind"/> of the expression, member, or parameter
        /// (i.e. <see cref="RefKind.Ref"/>, <see cref="RefKind.Out"/>, ...).
        /// </summary>
        RefKind RefKind { get; }
    }
}