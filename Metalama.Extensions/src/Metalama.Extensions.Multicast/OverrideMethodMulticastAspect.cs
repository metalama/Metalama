// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Extensions.Multicast;

/// <summary>
/// An aspect equivalent to <see cref="OverrideMethodAspect"/> that also implements multicasting for backward compatibility with PostSharp.
/// </summary>
/// <remarks>
/// <para>
/// This class combines the method overriding capabilities of <see cref="OverrideMethodAspect"/> with the multicasting features
/// provided by <see cref="MulticastAspect"/>. It can be applied to methods, properties, events, types, or assemblies, and will
/// automatically apply to matching method accessors based on the multicasting filter properties inherited from <see cref="IMulticastAttribute"/>.
/// </para>
/// <para>
/// For details on the template system, template selection, and best practices, see the documentation of <see cref="OverrideMethodAspect"/>.
/// </para>
/// </remarks>
/// <seealso cref="OverrideMethodAspect"/>
/// <seealso cref="MulticastAspect"/>
/// <seealso cref="IMulticastAttribute"/>
/// <seealso cref="OverrideFieldOrPropertyMulticastAspect"/>
/// <seealso href="@migrating-multicasting"/>
[AttributeUsage(
    AttributeTargets.Method | AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Event,
    AllowMultiple = true )]
[PublicAPI]
public abstract class OverrideMethodMulticastAspect : MulticastAspect, IAspect<IProperty>,
                                                      IAspect<IEvent>, IAspect<IMethod>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OverrideMethodMulticastAspect"/> class.
    /// </summary>
    protected OverrideMethodMulticastAspect() : base( MulticastTargets.Method ) { }

    /// <inheritdoc />
    public virtual void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        this.Implementation.BuildAspect(
            builder,
            b =>
            {
#if NET5_0_OR_GREATER
                var templates = new MethodTemplateSelector(
                    nameof(this.OverrideMethod),
                    nameof(this.OverrideAsyncMethod),
                    nameof(this.OverrideEnumerableMethod),
                    nameof(this.OverrideEnumeratorMethod),
                    nameof(this.OverrideAsyncEnumerableMethod),
                    nameof(this.OverrideAsyncEnumeratorMethod),
                    this.UseAsyncTemplateForAnyAwaitable,
                    this.UseEnumerableTemplateForAnyEnumerable );
#else
                var templates = new MethodTemplateSelector(
                    nameof(this.OverrideMethod),
                    nameof(this.OverrideAsyncMethod),
                    nameof(this.OverrideEnumerableMethod),
                    nameof(this.OverrideEnumeratorMethod),
                    null,
                    null,
                    this.UseAsyncTemplateForAnyAwaitable,
                    this.UseEnumerableTemplateForAnyEnumerable );
#endif

                b.Override( templates );
            } );
    }

    /// <inheritdoc />
    public virtual void BuildEligibility( IEligibilityBuilder<IProperty> builder )
    {
        builder.MustBeExplicitlyDeclared();
        this.BuildEligibility( builder.DeclaringType() );
    }

    /// <inheritdoc />
    public virtual void BuildAspect( IAspectBuilder<IProperty> builder )
    {
        this.Implementation.BuildAspect( builder );
    }

    /// <inheritdoc />
    public virtual void BuildEligibility( IEligibilityBuilder<IEvent> builder )
    {
        this.BuildEligibility( builder.DeclaringType() );
    }

    /// <inheritdoc />
    public virtual void BuildAspect( IAspectBuilder<IEvent> builder )
    {
        this.Implementation.BuildAspect( builder );
    }

    /// <summary>
    /// Gets or sets a value indicating whether enumerable templates should be selected based on return type rather than the <c>yield</c> modifier.
    /// See <see cref="OverrideMethodAspect"/> for details.
    /// </summary>
    protected bool UseEnumerableTemplateForAnyEnumerable { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether async templates should be selected based on return type rather than the <c>async</c> modifier.
    /// See <see cref="OverrideMethodAspect"/> for details.
    /// </summary>
    protected bool UseAsyncTemplateForAnyAwaitable { get; init; }

    public virtual void BuildEligibility( IEligibilityBuilder<IMethod> builder )
    {
        this.BuildEligibility( builder.DeclaringType() );
        builder.AddRule( EligibilityRuleFactory.GetAdviceEligibilityRule( AdviceKind.OverrideMethod ) );
    }

    /// <summary>
    /// Template for overriding asynchronous methods. See <see cref="OverrideMethodAspect.OverrideAsyncMethod"/> for details.
    /// </summary>
    [Template( IsEmpty = true )]
    public virtual Task<dynamic?> OverrideAsyncMethod() => throw new NotSupportedException();

    /// <summary>
    /// Template for overriding methods returning <see cref="IEnumerable{T}"/>. See <see cref="OverrideMethodAspect.OverrideEnumerableMethod"/> for details.
    /// </summary>
    [Template( IsEmpty = true )]
    public virtual IEnumerable<dynamic?> OverrideEnumerableMethod() => throw new NotSupportedException();

    /// <summary>
    /// Template for overriding methods returning <see cref="IEnumerator{T}"/>. See <see cref="OverrideMethodAspect.OverrideEnumeratorMethod"/> for details.
    /// </summary>
    [Template( IsEmpty = true )]
    public virtual IEnumerator<dynamic?> OverrideEnumeratorMethod() => throw new NotSupportedException();

#if NET5_0_OR_GREATER
    /// <summary>
    /// Template for overriding methods returning <see cref="IAsyncEnumerable{T}"/>. See <see cref="OverrideMethodAspect"/> for details.
    /// </summary>
    [Template( IsEmpty = true )]
    public virtual IAsyncEnumerable<dynamic?> OverrideAsyncEnumerableMethod() => throw new NotSupportedException();

    /// <summary>
    /// Template for overriding methods returning <see cref="IAsyncEnumerator{T}"/>. See <see cref="OverrideMethodAspect"/> for details.
    /// </summary>
    [Template( IsEmpty = true )]
    public virtual IAsyncEnumerator<dynamic?> OverrideAsyncEnumeratorMethod() => throw new NotSupportedException();
#endif

    /// <summary>
    /// The default template for overriding method implementations. See <see cref="OverrideMethodAspect.OverrideMethod"/> for details.
    /// </summary>
    [Template]
    public abstract dynamic? OverrideMethod();
}