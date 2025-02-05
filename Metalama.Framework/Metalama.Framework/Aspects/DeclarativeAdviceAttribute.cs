// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// A base class for attributes that define declarative advice members. 
/// </summary>
[CompileTime]
public abstract class DeclarativeAdviceAttribute : Attribute, IAdviceAttribute
{
    /// <summary>
    /// Gets or sets the name of the aspect layer into which the member will be introduced. The layer must have been defined
    /// using the <see cref="LayersAttribute"/> custom attribute.
    /// </summary>
    [PublicAPI]
    public string? Layer { get; set; }

    /// <summary>
    /// Builds the eligibility of an aspect that contains the current declarative advice.
    /// </summary>
    public virtual void BuildAspectEligibility( IEligibilityBuilder<IDeclaration> builder, IMemberOrNamedType adviceMember ) { }

    /// <summary>
    /// Builds the aspect, i.e. translates the current declarative advice into a programmatic advice or possibly diagnostics
    /// and validators. In case of error, the implementation must report diagnostics and call <see cref="IAspectBuilder.SkipAspect"/>.
    /// </summary>
    /// <param name="templateMember">The member or type to which the current attribute is applied.</param>
    /// <param name="templateMemberId">The a value that represents <paramref name="templateMember"/> and that must be supplied to <see cref="IAdviceFactory"/>.
    ///     It is not actually the name, but a unique identifier of <paramref name="templateMember"/>.</param>
    /// <param name="builder">An <see cref="IAspectBuilder{TAspectTarget}"/>.</param>
    public abstract void BuildAdvice( IMemberOrNamedType templateMember, string templateMemberId, IAspectBuilder<IDeclaration> builder );
}