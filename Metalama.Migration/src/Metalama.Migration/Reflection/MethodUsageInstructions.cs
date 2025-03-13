// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;

namespace PostSharp.Reflection
{
    /// <summary>
    /// In Metalama, use <see cref="ReferenceKinds"/>.
    /// </summary>
    [Flags]
    public enum MethodUsageInstructions
    {
        None,

        LoadField = 1,

        StoreField = 2,

        Call = 4,

        CallVirtual = 8,

        NewObject = 16,

        LoadFieldAddress = 32,

        LoadMetadata = 64,

        LoadMethodAddress = 128,

        LoadMethodAddressVirtual = 0x100,

        Cast = 0x200,

        IsInstance = 0x400,

        SizeOf = 0x800,

        NewArray = 0x1000
    }
}