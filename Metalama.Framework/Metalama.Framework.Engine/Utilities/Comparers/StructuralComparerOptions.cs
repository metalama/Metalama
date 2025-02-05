// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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