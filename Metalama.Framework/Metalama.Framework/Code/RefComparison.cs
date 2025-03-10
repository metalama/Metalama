// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code;

/// <summary>
/// Defines strategies to compare two instances of the <see cref="IRef{T}"/> interface.
/// </summary>
public enum RefComparison
{
    /// <summary>
    /// Does not support cross-compilation comparisons and ignores nullability when comparing <c>IRef{IType}</c>.
    /// </summary>
    Default,

    /// <summary>
    /// Does not support cross-compilation comparisons and respects nullability when comparing <c>IRef{IType}</c>.
    /// </summary>
    IncludeNullability,

    /// <summary>
    /// Support cross-compilation comparisons and ignores nullability when comparing <c>IRef{IType}</c>.
    /// </summary>
    Structural,

    /// <summary>
    /// Support cross-compilation comparisons and respects nullability when comparing <c>IRef{IType}</c>.
    /// </summary>
    StructuralIncludeNullability
}