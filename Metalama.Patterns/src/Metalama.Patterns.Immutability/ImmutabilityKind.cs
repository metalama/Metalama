// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Immutability;

[RunTimeOrCompileTime]
public enum ImmutabilityKind
{
    /// <summary>
    /// The type is mutable.
    /// </summary>
    None,

    /// <summary>
    /// The type itself is immutable, but some of its fields or properties may contain mutable objects.
    /// </summary>
    Shallow,

    /// <summary>
    /// The type and all values assigned to its fields and properties are deeply immutable. 
    /// </summary>
    Deep
}