// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Aspects;

/// <summary>
/// Specifies the direction of the data flow to which a contract applies. Contracts can validate or transform values
/// at different points in the data flow: when values are received (input/preconditions), when values are returned (output/postconditions), or both.
/// </summary>
/// <remarks>
/// <para>
/// Understanding contract directions is crucial for implementing effective validation:
/// </para>
/// <list type="bullet">
/// <item><description><b>Input contracts</b> validate values coming into a method or being assigned to a property (preconditions).</description></item>
/// <item><description><b>Output contracts</b> validate values being returned from a method or property getter (postconditions).</description></item>
/// <item><description><b>Both</b> applies validation in both directions, useful for ref parameters.</description></item>
/// </list>
/// <para>
/// The <see cref="Default"/> direction automatically selects the appropriate direction based on the target declaration type.
/// </para>
/// </remarks>
/// <seealso cref="ContractAspect"/>
/// <seealso cref="ContractAspect.GetDefinedDirection"/>
/// <seealso cref="AdviserExtensions.AddContract(Metalama.Framework.Aspects.IAdviser{Metalama.Framework.Code.IParameter}, string, Metalama.Framework.Aspects.ContractDirection, object?, object?)"/>
/// <seealso href="@contracts"/>
[RunTimeOrCompileTime]
public enum ContractDirection
{
    /// <summary>
    /// Disables the contract. The contract will not be applied.
    /// </summary>
    None,

    /// <summary>
    /// Uses the default direction based on the target declaration type:
    /// <list type="bullet">
    /// <item><description>For input and <c>ref</c> parameters: equivalent to <see cref="Input"/> (validates the value passed by the caller).</description></item>
    /// <item><description>For fields and properties: equivalent to <see cref="Input"/> (validates the value being assigned via the setter).</description></item>
    /// <item><description>For <c>out</c> parameters and return values: equivalent to <see cref="Output"/> (validates the value being returned to the caller).</description></item>
    /// <item><description>For read-only properties or indexers: equivalent to <see cref="Output"/> (validates the value returned by the getter).</description></item>
    /// </list>
    /// </summary>
    Default,

    /// <summary>
    /// Validates the input data flow (precondition):
    /// <list type="bullet">
    /// <item><description>For parameters: validates the value <i>before</i> the method executes (caller must provide valid value).</description></item>
    /// <item><description>For properties, indexers, and fields: validates the value <i>before</i> it is assigned (caller must assign valid value).</description></item>
    /// </list>
    /// This is commonly used for parameter validation (e.g., null checks, range validation).
    /// </summary>
    Input,

    /// <summary>
    /// Validates the output data flow (postcondition):
    /// <list type="bullet">
    /// <item><description>For <c>out</c> or <c>ref</c> parameters: validates the value <i>after</i> the method executes (method must set valid value).</description></item>
    /// <item><description>For return values: validates the value being returned (method must return valid value).</description></item>
    /// <item><description>For property or indexer getters: validates the value being retrieved (getter must return valid value).</description></item>
    /// <item><description>For fields: validates the value when the field is read (field must contain valid value).</description></item>
    /// </list>
    /// This is commonly used for return value validation and ensuring methods fulfill their contracts.
    /// </summary>
    Output,

    /// <summary>
    /// Applies to both <see cref="Input"/> and <see cref="Output"/> data flows. The contract validates:
    /// <list type="bullet">
    /// <item><description>Values coming in (preconditions)</description></item>
    /// <item><description>Values going out (postconditions)</description></item>
    /// </list>
    /// This is particularly useful for <c>ref</c> parameters where the value flows in both directions.
    /// </summary>
    Both
}