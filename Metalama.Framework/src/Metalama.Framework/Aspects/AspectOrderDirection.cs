// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Aspects;

/// <summary>
/// Specifies the direction in which aspect types or aspect layers are ordered in <see cref="AspectOrderAttribute"/>.
/// Choose between run-time execution order (outside-in) or compile-time application order (inside-out).
/// </summary>
/// <remarks>
/// <para>
/// Metalama follows the "matryoshka doll" model: source code is the innermost piece, and aspects are layered around it.
/// At build time, aspects are applied from the inside out. At runtime, aspects execute from the outside in.
/// Therefore, build-time order and run-time order are opposite.
/// </para>
/// </remarks>
/// <seealso cref="AspectOrderAttribute"/>
/// <seealso href="@ordering-aspects"/>
public enum AspectOrderDirection
{
    /// <summary>
    /// Specifies the run-time execution order (outside-in), which is more intuitive to aspect users.
    /// The first aspect in the list executes first at runtime.
    /// Prior to Metalama 2024.2, this was the only available option.
    /// </summary>
    /// <example>
    /// <c>[assembly: AspectOrder(AspectOrderDirection.RunTime, typeof(Cache), typeof(Logging), typeof(Retry))]</c>
    /// means Cache executes first at runtime, then Logging, then Retry.
    /// </example>
    RunTime,

    /// <summary>
    /// Specifies the compile-time application order (inside-out), which is more intuitive to aspect authors.
    /// The first aspect in the list is applied first at compile time (closest to the source code).
    /// </summary>
    /// <example>
    /// <c>[assembly: AspectOrder(AspectOrderDirection.CompileTime, typeof(Retry), typeof(Logging), typeof(Cache))]</c>
    /// means Retry is applied first (innermost), then Logging, then Cache (outermost). At run-time, the aspects are typically executed in inverse 
    /// order, i.e. first Cache, then Logging, then Retry.
    /// </example>
    CompileTime
}