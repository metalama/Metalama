// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Options;

namespace Metalama.Patterns.Contracts;

#pragma warning disable SA1623

/// <summary>
/// Options for all aspects of the <c>Metalama.Patterns.Contracts</c> namespace. You can configure these options
/// using the <see cref="ContractConfigurationExtensions.ConfigureContracts(Framework.Fabrics.IQuery{ICompilation}, ContractOptions)"/>
/// method.
/// </summary>
/// <seealso cref="ContractConfigurationExtensions"/>
[PublicAPI]
public sealed class ContractOptions : IHierarchicalOptions<ICompilation>, IHierarchicalOptions<INamespace>, IHierarchicalOptions<INamedType>,
                                      IHierarchicalOptions<IFieldOrPropertyOrIndexer>,
                                      IHierarchicalOptions<IMethod>, IHierarchicalOptions<IParameter>
{
    /// <summary>
    /// Gets or sets the code templates that are used by the aspects. You can set this property to an instance of a class that derives from <see cref="ContractTemplates"/>
    /// and overrides some of the virtual methods.
    /// </summary>
    public ContractTemplates? Templates { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the contracts are inheritable by default. The default value is <c>true</c>. This option can also be configured
    /// for each aspect using the <see cref="ContractBaseAttribute.IsInheritable"/> property of the aspect class.
    /// </summary>
    public bool? IsInheritable { get; init; }

    /// <summary>
    /// Gets or sets the default direction of contracts. The default value is <see cref="ContractDirection.Default"/>, which has a different behavior
    /// according to the declaration to which it is applied. This option can also be configured for each aspect using the <see cref="ContractBaseAttribute.Direction"/>
    /// property of the aspect class.
    /// </summary>
    public ContractDirection? Direction { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the LAMA5002 warning should be reported when attempting to add a <see cref="NotNullAttribute"/> or
    /// <see cref="RequiredAttribute"/> contract on a nullable parameter, field or property. This option is <c>true</c> by default.
    /// </summary>
    public bool? WarnOnNotNullableOnNullable { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether precondition contracts are being enforced. This option is <c>true</c> by default.
    /// </summary>
    public bool? ArePreconditionsEnabled { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether postcondition contracts are being enforced. This option is <c>true</c> by default.
    /// </summary>
    public bool? ArePostconditionsEnabled { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether invariants are being enforced. This option is <c>true</c> by default.
    /// </summary>
    public bool? AreInvariantsEnabled { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="InvariantAttribute"/> aspect should generate the code that
    /// supports the <see cref="SuspendInvariantsAttribute"/>, including the <c>SuspendInvariants</c> and <c>AreInvariantsSuspended</c>
    /// methods. The option is <c>false</c> by default.
    /// </summary>
    public bool? IsInvariantSuspensionSupported { get; init; }

    /// <summary>
    /// Gets or sets a value determining the meaning of inequalities whose strictness is not explicitly specified
    /// i.e. <see cref="GreaterThanAttribute"/>, <see cref="LessThanAttribute"/>, <see cref="PositiveAttribute"/> and <see cref="NegativeAttribute"/>.
    /// </summary>
    public InequalityStrictness? DefaultInequalityStrictness { get; init; }

    object IIncrementalObject.ApplyChanges( object changes, in ApplyChangesContext context )
    {
        var other = (ContractOptions) changes;

        return new ContractOptions
        {
            Templates = other.Templates ?? this.Templates,
            IsInheritable = other.IsInheritable ?? this.IsInheritable,
            Direction = other.Direction ?? this.Direction,
            WarnOnNotNullableOnNullable = other.WarnOnNotNullableOnNullable ?? this.WarnOnNotNullableOnNullable,
            AreInvariantsEnabled = other.AreInvariantsEnabled ?? this.AreInvariantsEnabled,
            ArePostconditionsEnabled = other.ArePostconditionsEnabled ?? this.ArePostconditionsEnabled,
            ArePreconditionsEnabled = other.ArePreconditionsEnabled ?? this.ArePreconditionsEnabled,
            IsInvariantSuspensionSupported = other.IsInvariantSuspensionSupported ?? this.IsInvariantSuspensionSupported,
            DefaultInequalityStrictness = other.DefaultInequalityStrictness ?? this.DefaultInequalityStrictness
        };
    }

    IHierarchicalOptions IHierarchicalOptions.GetDefaultOptions( OptionsInitializationContext context )
        => new ContractOptions { Templates = new ContractTemplates() };
}