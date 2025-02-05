// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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