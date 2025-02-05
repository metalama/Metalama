// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Engine.CodeModel.Helpers
{
    [Flags]
    internal enum ModifierCategories
    {
        Accessibility = 1,
        Inheritance = 2,
        Async = 4,
        Static = 8,
        ReadOnly = 16,
        Unsafe = 32,
        Volatile = 64,
        Required = 128,
        Const = 256,
        Partial = 512,
        Extern = 1024,
        All = Accessibility | Inheritance | Async | Static | ReadOnly | Unsafe | Volatile | Required | Const | Partial | Extern
    }
}