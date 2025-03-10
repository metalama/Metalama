// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Engine.CompileTime.Serialization
{
    internal enum SerializationIntrinsicType : byte
    {
        None,
        Byte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Single,
        Double,
        String,
        DottedString,
        Char,
        Boolean,
        SByte,
        Struct,
        Class,
        Array,
        ObjRef,
        Type,
        GenericTypeParameter,

        // Resharper disable UnusedMember.Global
        [Obsolete]
        Unused1,

        Enum
    }
}