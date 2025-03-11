// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using System.Collections.Immutable;

namespace Metalama.Extensions.DependencyInjection.Implementation;

/// <summary>
/// Specifies a constructor parameter.
/// </summary>
[CompileTime]
public readonly struct ParameterSpecification
{
    /// <summary>
    /// Gets the parameter name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the parameter type.
    /// </summary>
    public IType Type { get; }

    /// <summary>
    /// Gets the list of custom attributes of the parameters.
    /// </summary>
    public ImmutableArray<AttributeConstruction> Attributes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterSpecification"/> struct.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <param name="type">Parameter type.</param>
    /// <param name="attributes">List of custom attributes of the parameter.</param>
    public ParameterSpecification( string name, IType type, ImmutableArray<AttributeConstruction> attributes = default )
    {
        this.Name = name;
        this.Type = type;
        this.Attributes = attributes.IsDefault ? ImmutableArray<AttributeConstruction>.Empty : attributes;
    }
}