// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// A base class for attributes that define declarative advice on aspect members. These attributes enable
/// aspects to transform code without requiring explicit calls in <see cref="IAspect{T}.BuildAspect"/>.
/// </summary>
/// <remarks>
/// <para>
/// Declarative advice attributes allow you to mark members of an aspect class (methods, properties, fields, events)
/// with attributes that automatically generate advice when the aspect is applied. This is in contrast to imperative
/// advice, where transformations are explicitly added in the <see cref="IAspect{T}.BuildAspect"/> method via
/// <see cref="AdviserExtensions"/>.
/// </para>
/// <para>
/// Built-in declarative advice attributes include:
/// <list type="bullet">
/// <item><description><see cref="IntroduceAttribute"/>: Introduces the annotated member into the target type</description></item>
/// <item><description><c>IntroduceDependencyAttribute</c> (in Metalama.Extensions.DependencyInjection): Introduces a dependency as a property or field</description></item>
/// </list>
/// Derived attributes can implement more complex advice patterns.
/// </para>
/// <para>
/// Use the <see cref="Layer"/> property to control when the advice executes relative to other advice in the same aspect.
/// This is useful when an aspect has multiple pieces of advice that must execute in a specific order.
/// </para>
/// </remarks>
/// <seealso cref="IntroduceAttribute"/>
/// <seealso cref="AdviserExtensions"/>
/// <seealso cref="LayersAttribute"/>
/// <seealso href="@advising-concepts"/>
/// <seealso href="@introducing-members"/>
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