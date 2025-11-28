// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Advising;

/* The benefits of the design of having each advice kind return its own IAdviceResult interface are that:
    - it is possible to build fluent APIs based on advice results
    - it is possible to extend the interfaces with more properties
*/

/// <summary>
/// Represents the result of applying an advice (code transformation) through <see cref="AdviserExtensions"/> methods.
/// Check the <see cref="Outcome"/> property to determine whether the advice was successfully applied.
/// </summary>
/// <remarks>
/// <para>
/// All advice methods in <see cref="AdviserExtensions"/> return an <see cref="IAdviceResult"/> or a derived interface.
/// The result provides information about:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="AdviceKind"/>: The type of transformation attempted (override, introduce, etc.)</description></item>
/// <item><description><see cref="Outcome"/>: Whether the advice succeeded, was ignored, or encountered an error</description></item>
/// </list>
/// <para>
/// Derived result interfaces (e.g., <see cref="IIntroductionAdviceResult{T}"/>) provide additional properties
/// such as the introduced declaration or specific outcome details.
/// </para>
/// <para>
/// <b>Typical usage:</b> Check the <see cref="Outcome"/> property after applying advice to determine if follow-up
/// actions are needed or to conditionally proceed based on success/failure.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = builder.IntroduceMethod(nameof(MyMethod));
/// if (result.Outcome == AdviceOutcome.Success)
/// {
///     // Use result.Declaration to reference the introduced method
/// }
/// </code>
/// </example>
/// <seealso cref="IIntroductionAdviceResult{T}"/>
/// <seealso cref="IOverrideAdviceResult{T}"/>
/// <seealso cref="IImplementInterfaceAdviceResult"/>
/// <seealso cref="IAddContractAdviceResult{T}"/>
/// <seealso cref="IAddInitializerAdviceResult"/>
/// <seealso cref="IRemoveAttributesAdviceResult"/>
/// <seealso cref="AdviceOutcome"/>
/// <seealso cref="AdviceKind"/>
/// <seealso cref="AdviserExtensions"/>
/// <seealso href="@advising-code"/>
[CompileTime]
[InternalImplement]
public interface IAdviceResult
{
    /// <summary>
    /// Gets the kind of advice whose the current object is the result.
    /// </summary>
    AdviceKind AdviceKind { get; }

    /// <summary>
    /// Gets the advice outcome, indicating whether the advice was applied, was ignored because the same declaration already exists (according to <see cref="OverrideStrategy"/>),
    /// or resulted in an error diagnostic that caused the aspect to be skipped.
    /// </summary>
    AdviceOutcome Outcome { get; }
}