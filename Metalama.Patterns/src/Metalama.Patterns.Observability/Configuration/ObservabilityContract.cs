// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;

namespace Metalama.Patterns.Observability.Configuration;

/// <summary>
/// Represents behavioral guarantees made by a method, field, or property with respect to the <see cref="ObservableAttribute"/> aspect.
/// </summary>
/// <remarks>
/// <para>
/// An observability contract allows you to inform the <see cref="ObservableAttribute"/> aspect about the behavior of
/// methods it cannot analyze automatically. This prevents false positive warnings and enables correct dependency tracking.
/// </para>
/// </remarks>
/// <seealso cref="ObservableAttribute"/>
/// <seealso cref="ConstantAttribute"/>
/// <seealso cref="ObservabilityTypeOptionsBuilder.ObservabilityContract"/>
/// <seealso href="@observability"/>
[CompileTime]
public sealed class ObservabilityContract : ICompileTimeSerializable
{
    private ObservabilityContract() { }

    /// <summary>
    /// Gets an <see cref="ObservabilityContract"/> that guarantees that the outputs of a method depend only on its input
    /// parameters (i.e., identical inputs will always produce identical outputs) and that the method has no side effects
    /// on observable state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When applied to a type, the guarantee must hold for all methods and properties within that type.
    /// </para>
    /// </remarks>
    public static ObservabilityContract Constant { get; } = new();
}