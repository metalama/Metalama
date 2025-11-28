// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Specifies type kind constraints for generic type parameters.
    /// </summary>
    /// <seealso cref="ITypeParameter"/>
    /// <seealso cref="IGeneric"/>
    [CompileTime]
    public enum TypeKindConstraint
    {
        None,
        Class,
        Struct,
        Unmanaged,
        NotNull,
        Default
    }
}