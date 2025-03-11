// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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