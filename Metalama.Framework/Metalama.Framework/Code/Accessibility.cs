// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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