// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Aspects;

/// <summary>
/// Directions of the data flow to which the contract applies.
/// </summary>
[RunTimeOrCompileTime]
public enum ContractDirection
{
    /// <summary>
    /// Means that the contract is disabled.
    /// </summary>
    None,

    /// <summary>
    /// For all parameters except <c>out</c> parameters and read-only properties or indexers, equivalent to <see cref="Input"/>. Otherwise, equivalent to <see cref="Output"/>. 
    /// </summary>
    Default,

    /// <summary>
    /// Validates the input value of the parameter (before execution of the method) or the value assigned to the field, property or indexer (before the actual assignment).
    /// </summary>
    Input,

    /// <summary>
    /// Validates the output value of an <c>out</c> or <c>ref</c> parameter or the value (after execution of the method), the value returned by the
    /// property or indexer getter, or the value assigned to the field at the moment when the field is retrieved.
    /// </summary>
    Output,

    /// <summary>
    /// Both <see cref="Input"/> and <see cref="Output"/>.
    /// </summary>
    Both
}