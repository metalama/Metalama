// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;

namespace Metalama.Patterns.Observability.Configuration;

/// <summary>
/// Represents guarantees made by a method, field or property with regards to the <see cref="ObservableAttribute"/> aspect.
/// </summary>
[CompileTime]
public sealed class ObservabilityContract : ICompileTimeSerializable
{
    private ObservabilityContract() { }

    /// <summary>
    /// Gets an <see cref="ObservabilityContract"/> that guarantees that the outputs of the member (1) do depend on any non-constant
    /// input (i.e. will always be identical given identical inputs) and (2) are themselves constant.
    /// When applied to a type, the guarantee must hold for all methods and properties.
    /// </summary>
    public static ObservabilityContract Constant { get; } = new();
}