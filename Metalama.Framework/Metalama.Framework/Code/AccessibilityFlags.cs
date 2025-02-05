// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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