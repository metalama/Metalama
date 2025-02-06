// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Represents the context in which a <see cref="ContractBaseAttribute"/> is used,
/// i.e. its target declaration and <see cref="Options"/>.
/// </summary>
[CompileTime]
[PublicAPI]
public sealed class ContractContext
{
    public ContractContext( IMetaTarget target ) : this( target.Declaration, target.ContractDirection ) { }

    public ContractContext( IDeclaration target, ContractDirection direction )
    {
        this.TargetDeclaration = target;
        this.Options = target.GetContractOptions();
        this.Direction = direction;
    }

    /// <summary>
    /// Gets the declaration (parameter, field or property) validated by the contract.
    /// </summary>
    public IDeclaration TargetDeclaration { get; }

    /// <summary>
    /// Gets the application options.
    /// </summary>
    public ContractOptions Options { get; }

    /// <summary>
    /// Gets the type of the <see cref="TargetDeclaration"/>.
    /// </summary>
    public IType Type => ((IHasType) this.TargetDeclaration).Type;
    
    /// <summary>
    /// Gets the <see cref="ContractDirection"/>.
    /// </summary>
    public ContractDirection Direction { get; }

    /// <summary>
    /// Gets the display name of the <see cref="TargetDeclaration"/>.
    /// </summary>
    public string TargetDisplayName
        => this.TargetDeclaration.DeclarationKind switch
        {
            DeclarationKind.Parameter when ((IParameter) this.TargetDeclaration).IsReturnParameter => "return value",
            DeclarationKind.Parameter => $"'{((IParameter) this.TargetDeclaration).Name}' parameter",
            DeclarationKind.Property => $"'{((IProperty) this.TargetDeclaration).Name}' property",
            DeclarationKind.Field => $"'{((IField) this.TargetDeclaration).Name}' field",
            DeclarationKind.Indexer => "indexer",
            _ => throw new ArgumentOutOfRangeException(
                nameof(this.TargetDeclaration) + "." + nameof(this.TargetDeclaration) + "." +
                nameof(this.TargetDeclaration.DeclarationKind) )
        };

    /// <summary>
    /// Gets the name of the target parameter, or <c>value</c> if the target declaration is a property.
    /// </summary>
    public string TargetParameterName
        => this.TargetDeclaration.DeclarationKind switch
        {
            DeclarationKind.Parameter => ((IParameter) this.TargetDeclaration).Name,
            DeclarationKind.Property or DeclarationKind.Field or DeclarationKind.Indexer => "value",
            _ => throw new ArgumentOutOfRangeException(
                nameof(this.TargetDeclaration) + "." + nameof(this.TargetDeclaration) + "." +
                nameof(this.TargetDeclaration.DeclarationKind) )
        };
}