// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Extensions.Multicast;

/// <summary>
/// A base class that can be used to build aspects that mimics PostSharp attribute multicasting. This class implements multicasting
/// from the assembly- or type-level custom attributes. Implementations of this class must provide the other implementations of the
/// <see cref="IAspect{T}"/> generic interface for the final type of declarations.
/// </summary>
public abstract class MulticastAspect : Aspect, IMulticastAttribute, IAspect<ICompilation>, IAspect<INamedType>
{
    internal static readonly DiagnosticDefinition<(string PropertyName, string AspectName, DeclarationKind DeclarationKind, IDeclaration Declaration, string
            Message)>
        InvalidRegexError
            = new(
                "PS0093",
                Severity.Error,
                "Invalid expression for property {0} of custom attribute {1} on {2} {3}: {4}",
                "Invalid regular expression.",
                "Metalama.Extensions.Multicast" );

    private readonly MulticastTargets _targets;
    private readonly bool _multicastOnInheritance;

    [NonCompileTimeSerialized]
    private MulticastImplementation? _implementation;

    /// <summary>
    /// Initializes a new instance of the <see cref="MulticastAspect"/> class.
    /// </summary>
    /// <param name="targets">The set of targets that this aspect can be multicast to. The aspect class must also implement the corresponding generic
    /// instances of the <see cref="IAspect{T}"/> interface.</param>
    /// <param name="multicastOnInheritance">A value indicating whether an aspect instance, when it is inherited, should also multicast to children. The default is <c>false</c>.
    /// It corresponds to the <see cref="MulticastInheritance.Strict"/> multicast inheritance mode in PostSharp. When set to <c>true</c>, the behavior is equivalent to <see cref="MulticastInheritance.Multicast"/>.
    /// </param>
    protected MulticastAspect( MulticastTargets targets, bool multicastOnInheritance = false )
    {
        this._targets = targets;
        this._multicastOnInheritance = multicastOnInheritance;
    }

    protected MulticastImplementation Implementation => this._implementation ??= new MulticastImplementation( this._targets, this._multicastOnInheritance );

    /// <inheritdoc />
    public MulticastTargets AttributeTargetElements { get; set; }

    /// <inheritdoc />
    [Obsolete]
    public string? AttributeTargetAssemblies { get; set; }

    /// <inheritdoc />
    public string? AttributeTargetTypes { get; set; }

    /// <inheritdoc />
    public MulticastAttributes AttributeTargetTypeAttributes { get; set; }

    /// <inheritdoc />
    [Obsolete( ObsoleteMessages.ExternalAssemblies, ObsoleteMessages.Error )]
    public MulticastAttributes AttributeTargetExternalTypeAttributes { get; set; }

    /// <inheritdoc />
    public string? AttributeTargetMembers { get; set; }

    /// <inheritdoc />
    public MulticastAttributes AttributeTargetMemberAttributes { get; set; }

    /// <inheritdoc />
    [Obsolete( ObsoleteMessages.ExternalAssemblies, ObsoleteMessages.Error )]
    public MulticastAttributes AttributeTargetExternalMemberAttributes { get; set; }

    /// <inheritdoc />
    public string? AttributeTargetParameters { get; set; }

    /// <inheritdoc />
    public MulticastAttributes AttributeTargetParameterAttributes { get; set; }

    /// <inheritdoc />
    public bool AttributeExclude { get; set; }

    /// <inheritdoc />
    public int AttributePriority { get; set; }

    /// <inheritdoc />
    [Obsolete( ObsoleteMessages.AttributeReplace, ObsoleteMessages.Error )]
    public bool AttributeReplace { get; set; }

    /// <inheritdoc />
    [Obsolete( ObsoleteMessages.Inheritance, ObsoleteMessages.Error )]
    public MulticastInheritance AttributeInheritance { get; set; }

    /// <inheritdoc />
    public virtual void BuildEligibility( IEligibilityBuilder<ICompilation> builder ) { }

    /// <inheritdoc />
    public virtual void BuildAspect( IAspectBuilder<ICompilation> builder )
    {
        this.Implementation.BuildAspect( builder );
    }

    /// <inheritdoc />
    public virtual void BuildEligibility( IEligibilityBuilder<INamedType> builder ) { }

    /// <inheritdoc />
    public virtual void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        this.Implementation.BuildAspect( builder );
    }
}