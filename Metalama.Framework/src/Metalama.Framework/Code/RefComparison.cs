// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code;

/// <summary>
/// Defines strategies to compare two instances of the <see cref="IRef{T}"/> interface.
/// </summary>
/// <remarks>
/// <para>
/// References can be compared using different strategies depending on the scenario:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Use <see cref="Default"/> or <see cref="IncludeNullability"/> when comparing references
/// within the same compilation. These strategies are faster but do not work across different compilation versions.</description>
/// </item>
/// <item>
/// <description>Use <see cref="Structural"/> or <see cref="StructuralIncludeNullability"/> when comparing
/// references across different compilations. These strategies compare references by their structural identity
/// (e.g., fully qualified name) rather than object identity.</description>
/// </item>
/// </list>
/// <para>
/// For type references (<see cref="IRef{T}"/> where <c>T</c> is <see cref="IType"/>), the nullability variants
/// consider nullable reference type annotations in the comparison.
/// </para>
/// </remarks>
/// <seealso cref="IRef"/>
/// <seealso cref="Comparers.RefEqualityComparer{T}"/>
/// <seealso cref="Comparers.RefEqualityComparer"/>
public enum RefComparison
{
    /// <summary>
    /// Does not support cross-compilation comparisons and ignores nullability when comparing type references.
    /// This is the fastest comparison strategy and is suitable for comparing references within the same compilation.
    /// </summary>
    Default,

    /// <summary>
    /// Does not support cross-compilation comparisons and respects nullability when comparing type references.
    /// Use this strategy when nullable reference type annotations should affect equality.
    /// </summary>
    IncludeNullability,

    /// <summary>
    /// Supports cross-compilation comparisons and ignores nullability when comparing type references.
    /// Use this strategy when comparing references from different compilation versions, such as when
    /// tracking declarations across aspect pipeline steps.
    /// </summary>
    Structural,

    /// <summary>
    /// Supports cross-compilation comparisons and respects nullability when comparing type references.
    /// This is the most comprehensive comparison strategy, suitable for cross-compilation scenarios where
    /// nullable reference type annotations should affect equality.
    /// </summary>
    StructuralIncludeNullability
}