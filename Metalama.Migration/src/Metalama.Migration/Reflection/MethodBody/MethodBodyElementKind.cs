// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace PostSharp.Reflection.MethodBody
{
    /// <summary>
    /// The source code of method bodies and expression bodies is not represented in the high-level API of Metalama.
    /// If you need to access source code from an aspect, you must implement a service using Metalama SDK. 
    /// </summary>
    /// <seealso href="@roslyn-api"/>
    public enum MethodBodyElementKind
    {
        Add,

        AddChecked,

        And,

        ArrayIndex,

        ArrayLength,

        Assign,

        Goto,

        Variable,

        Conditional,

        Cast,

        Divide,

        LessThan,

        LessThanOrEqual,

        Modulo,

        MultiplyChecked,

        Multiply,

        GreaterThan,

        GreaterThanOrEqual,

        Substract,

        SubstractChecked,

        Parameter,

        This,

        AddressOf,

        Constant,

        Equal,

        Not,

        Different,

        ValueOf,

        Or,

        Xor,

        ShiftLeft,

        ShiftRight,

        Negate,

        SafeCast,

        Unbox,

        Throw,

        Field,

        Box,

        NewArray,

        TypedReferenceValue,

        CheckFinite,

        MakeTypedReference,

        ArgumentList,

        MethodPointer,

        LocalAlloc,

        DefaultValue,

        CopyBuffer,

        InitBuffer,

        LoadToken,

        SizeOf,

        TypedReferenceType,

        ConvertChecked,

        Convert,

        MethodCall,

        Return,

        NewObject,

        Switch,

        Block,

        MethodBody,

        LocalVariableDefinition,

        Statement,

        Rethrow
    }
}