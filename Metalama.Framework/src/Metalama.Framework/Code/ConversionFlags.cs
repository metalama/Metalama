// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code;

/// <summary>
/// Flags that modify the behavior of type conversion checks in <see cref="Comparers.ITypeComparer.IsConvertibleTo(IType, IType, ConversionKind, ConversionFlags)"/>.
/// </summary>
[Flags]
[CompileTime]
public enum ConversionFlags
{
    /// <summary>
    /// Default behavior. No special flags.
    /// </summary>
    Default = 0,

    /// <summary>
    /// When set, method-level type parameters are considered equivalent if they occupy the same ordinal position,
    /// regardless of which declaring method they belong to. This is useful when comparing method signatures from
    /// different declaring types, where their type parameters are distinct objects but represent the same positional slot.
    /// </summary>
    TypeParameterEquivalence = 1
}