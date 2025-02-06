// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Patterns.Contracts;

#pragma warning disable SA1623

/// <summary>
/// A base class for all contracts defined in this library.
/// </summary>
[PublicAPI]
public abstract class ContractBaseAttribute : ContractAspect, IConditionallyInheritableAspect
{
    private readonly bool? _isInheritable;
    private readonly ContractDirection? _direction;

    protected override ContractDirection GetDefinedDirection( IAspectBuilder builder )
        => this._direction ??
           builder.Target.GetContractOptions().Direction ?? ContractDirection.Default;

    protected override ContractDirection GetActualDirection( IAspectBuilder builder, ContractDirection direction )
    {
        var options = builder.Target.GetContractOptions();

        switch ( direction )
        {
            case ContractDirection.Input when options.ArePreconditionsEnabled == false:
                return ContractDirection.None;

            case ContractDirection.Output when options.ArePostconditionsEnabled == false:
                return ContractDirection.None;

            case ContractDirection.Both when options is { ArePreconditionsEnabled: false, ArePostconditionsEnabled: false }:
                return ContractDirection.None;

            case ContractDirection.Both when options.ArePostconditionsEnabled == false:
                return ContractDirection.Input;

            case ContractDirection.Both when options.ArePreconditionsEnabled == false:
                return ContractDirection.Output;

            default:
                return direction;
        }
    }

    /// <summary>
    /// Gets or sets the direction of the contract. When this property is not set, its
    /// default value is read from <see cref="ContractOptions"/>. When no value is defined
    /// in <see cref="ContractOptions"/>, the default value of this property is <see cref="ContractDirection.Default"/>.
    /// </summary>
    public ContractDirection Direction
    {
        get => this._direction ?? ContractDirection.Default;
        init => this._direction = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this contract should be inherited to derived types or overriding members.
    /// When this property is not set, its default value is read from <see cref="ContractOptions"/>. When no value
    /// is defined in <see cref="ContractOptions"/>, the default value of this property is <c>true</c>.
    /// </summary>
    public bool IsInheritable
    {
        get => this._isInheritable ?? true;
        init => this._isInheritable = value;
    }

    bool IConditionallyInheritableAspect.IsInheritable( IDeclaration targetDeclaration, IAspectInstance aspectInstance )
        => this._isInheritable ?? targetDeclaration.GetContractOptions().IsInheritable ?? true;
}