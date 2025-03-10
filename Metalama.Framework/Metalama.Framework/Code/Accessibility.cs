// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Accessibility of types and members, for instance <see cref="Private"/> or <see cref="Public"/>.
    /// </summary>
    [CompileTime]
    public enum Accessibility
    {
        // IMPORTANT: Don't change. Comparisons depend on the order.        
        Undefined = 0,
        Private = 1,
        PrivateProtected = 2,
        Protected = 3,
        Internal = 4,
        ProtectedInternal = 5,
        Public = 6
    }
}