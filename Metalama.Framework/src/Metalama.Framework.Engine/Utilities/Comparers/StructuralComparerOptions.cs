// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Engine.Utilities.Comparers
{
    [Flags]
    internal enum StructuralComparerOptions
    {
        ContainingAssembly = 1 << 0,
        ContainingDeclaration = 1 << 1,
        Name = 1 << 2,
        GenericParameterCount = 1 << 3,
        GenericArguments = 1 << 4,
        ParameterTypes = 1 << 5,
        ParameterModifiers = 1 << 6,
        Nullability = 1 << 7,

        MethodSignature = Name | GenericParameterCount | GenericArguments | ParameterTypes | ParameterModifiers,
        FunctionPointer = GenericParameterCount | GenericArguments | ParameterTypes | ParameterModifiers,
        Type = Name | GenericParameterCount | GenericArguments
    }
}