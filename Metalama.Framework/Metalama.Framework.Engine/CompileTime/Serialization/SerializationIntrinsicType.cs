// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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