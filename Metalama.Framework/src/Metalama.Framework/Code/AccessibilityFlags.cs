// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code
{
    [Flags]
    [CompileTime]
    public enum AccessibilityFlags
    {
        None = 0,
        SameType = 1,
        DerivedTypeOfFriendAssembly = 2,
        DerivedTypeOfAnyAssembly = 4,
        AnyTypeOfFriendAssembly = 8,
        AnyType = 16,
        Public = SameType | DerivedTypeOfAnyAssembly | DerivedTypeOfFriendAssembly | AnyTypeOfFriendAssembly | AnyType
    }
}