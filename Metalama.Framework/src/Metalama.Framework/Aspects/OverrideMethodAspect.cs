// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base class for aspects that override method implementations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class simplifies creating aspects that replace method implementations. Derived classes must implement the
    /// <see cref="OverrideMethod"/> template, which defines the new method implementation. The template has access to the
    /// original method through <c>meta.Proceed()</c>.
    /// </para>
    /// <para>
    /// This aspect automatically selects the appropriate template based on the target method's characteristics:
    /// <list type="bullet">
    /// <item><see cref="OverrideAsyncMethod"/> for <c>async</c> methods (or all awaitable types if <see cref="UseAsyncTemplateForAnyAwaitable"/> is <c>true</c>)</item>
    /// <item><see cref="OverrideEnumerableMethod"/> or <see cref="OverrideEnumeratorMethod"/> for iterator methods using <c>yield</c> (or all enumerable types if <see cref="UseEnumerableTemplateForAnyEnumerable"/> is <c>true</c>)</item>
    /// <item><c>OverrideAsyncEnumerableMethod</c> or <c>OverrideAsyncEnumeratorMethod</c> for async iterator methods (or all async enumerable types if both properties are <c>true</c>)</item>
    /// <item><see cref="OverrideMethod"/> for all other methods</item>
    /// </list>
    /// If specialized templates are not overridden, <see cref="OverrideMethod"/> is used as the fallback.
    /// </para>
    /// <para>
    /// The properties <see cref="UseAsyncTemplateForAnyAwaitable"/> and <see cref="UseEnumerableTemplateForAnyEnumerable"/>
    /// control template selection strategy. By default (<c>false</c>), templates are selected based on method modifiers
    /// (<c>async</c>, <c>yield</c>). When set to <c>true</c>, selection is based solely on return type, which is useful
    /// when you need to handle all methods of certain return types consistently.
    /// </para>
    /// </remarks>
    /// <seealso cref="MethodAspect"/>
    /// <seealso cref="MethodTemplateSelector"/>
    /// <seealso cref="AdviserExtensions.Override(IAdviser{IMethod}, in MethodTemplateSelector, object?, object?)"/>
    /// <seealso href="@overriding-methods"/>
    [AttributeUsage( AttributeTargets.Method )]
    [PublicAPI]
    public abstract class OverrideMethodAspect : MethodAspect
    {
        /// <inheritdoc />
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            // The .Net Standard version of Metalama.Framework is used at compile-time, which is why we have to specify async enumerable methods even then (and then ignore them if not present).
            var templates = new MethodTemplateSelector(
                nameof(this.OverrideMethod),
                nameof(this.OverrideAsyncMethod),
                nameof(this.OverrideEnumerableMethod),
                nameof(this.OverrideEnumeratorMethod),
                "OverrideAsyncEnumerableMethod",
                "OverrideAsyncEnumeratorMethod",
                this.UseAsyncTemplateForAnyAwaitable,
                this.UseEnumerableTemplateForAnyEnumerable );

            builder.Override( templates );
        }

#pragma warning disable SA1623

        /// <summary>
        /// Gets or sets a value indicating whether enumerable templates should be selected based on return type rather than the <c>yield</c> modifier.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>false</c> (default), the <see cref="OverrideEnumerableMethod"/> or <see cref="OverrideEnumeratorMethod"/> templates
        /// are used only for methods that use the <c>yield</c> statement.
        /// </para>
        /// <para>
        /// When <c>true</c>, these templates are used for <i>all</i> methods returning <see cref="System.Collections.Generic.IEnumerable{T}"/>
        /// or <see cref="System.Collections.Generic.IEnumerator{T}"/>, regardless of whether they use <c>yield</c>.
        /// </para>
        /// <para>
        /// This property is useful when you want to handle all methods returning enumerable types uniformly, even if they don't use iterators.
        /// </para>
        /// </remarks>
        protected bool UseEnumerableTemplateForAnyEnumerable { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether async templates should be selected based on return type rather than the <c>async</c> modifier.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>false</c> (default), the <see cref="OverrideAsyncMethod"/> template is used only for methods with the <c>async</c> modifier.
        /// </para>
        /// <para>
        /// When <c>true</c>, this template is used for <i>all</i> methods returning awaitable types (<see cref="System.Threading.Tasks.Task"/>,
        /// <see cref="System.Threading.Tasks.ValueTask"/>, <c>IAsyncEnumerable</c>, <c>IAsyncEnumerator</c>), regardless of the <c>async</c> modifier.
        /// </para>
        /// <para>
        /// This property is useful when you want to handle all methods returning awaitable types uniformly, even if they're not marked <c>async</c>.
        /// </para>
        /// </remarks>
        protected bool UseAsyncTemplateForAnyAwaitable { get; init; }
#pragma warning restore SA1623

        public override void BuildEligibility( IEligibilityBuilder<IMethod> builder )
        {
            builder.AddRule( EligibilityRuleFactory.OverrideMethodAdviceRule );
        }

        /// <summary>
        /// Template for overriding asynchronous methods.
        /// </summary>
        /// <remarks>
        /// <para>
        /// By default, this template is selected for methods with the <c>async</c> modifier. When <see cref="UseAsyncTemplateForAnyAwaitable"/>
        /// is set to <c>true</c>, it applies to all methods returning an awaitable type (<c>Task</c>, <c>ValueTask</c>, <c>IAsyncEnumerable</c>,
        /// <c>IAsyncEnumerator</c>).
        /// </para>
        /// <para>
        /// If not overridden in a derived class, <see cref="OverrideMethod"/> is used instead. This template is marked with
        /// <c>[Template(IsEmpty = true)]</c>, making it optional to override.
        /// </para>
        /// </remarks>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Template( IsEmpty = true )]
        public virtual Task<dynamic?> OverrideAsyncMethod() => throw new NotSupportedException();

        /// <summary>
        /// Template of the new method implementation for methods returning <see cref="IEnumerable{T}"/>. By default, this template is used for methods using the <c>yield</c> statement.
        /// When <see cref="UseEnumerableTemplateForAnyEnumerable"/> is <c>true</c>, this template is used for all methods returning <see cref="IEnumerable{T}"/>.
        /// If this template is not overridden, <see cref="OverrideMethod"/> is used instead.
        /// </summary>
        [Template( IsEmpty = true )]
        public virtual IEnumerable<dynamic?> OverrideEnumerableMethod() => throw new NotSupportedException();

        /// <summary>
        /// Template of the new method implementation for methods returning <see cref="IEnumerator{T}"/>. By default, this template is used for methods using the <c>yield</c> statement.
        /// When <see cref="UseEnumerableTemplateForAnyEnumerable"/> is <c>true</c>, this template is used for all methods returning <see cref="IEnumerator{T}"/>.
        /// If this template is not overridden, <see cref="OverrideMethod"/> is used instead.
        /// </summary>
        [Template( IsEmpty = true )]
        public virtual IEnumerator<dynamic?> OverrideEnumeratorMethod() => throw new NotSupportedException();

#if NET5_0_OR_GREATER
        /// <summary>
        /// Template of the new method implementation for methods returning <see cref="IAsyncEnumerable{T}"/>. By default, this template is used for methods using the <c>yield</c> statement with <c>async</c>.
        /// When <see cref="UseEnumerableTemplateForAnyEnumerable"/> is <c>true</c>, this template is used for all methods returning <see cref="IAsyncEnumerable{T}"/>.
        /// If this template is not overridden, <see cref="OverrideAsyncMethod"/> is used when <see cref="UseAsyncTemplateForAnyAwaitable"/> is <c>true</c>, otherwise <see cref="OverrideMethod"/> is used.
        /// </summary>
        [Template( IsEmpty = true )]
        public virtual IAsyncEnumerable<dynamic?> OverrideAsyncEnumerableMethod() => throw new NotSupportedException();

        /// <summary>
        /// Template of the new method implementation for methods returning <see cref="IAsyncEnumerator{T}"/>. By default, this template is used for methods using the <c>yield</c> statement with <c>async</c>.
        /// When <see cref="UseEnumerableTemplateForAnyEnumerable"/> is <c>true</c>, this template is used for all methods returning <see cref="IAsyncEnumerator{T}"/>.
        /// If this template is not overridden, <see cref="OverrideAsyncMethod"/> is used when <see cref="UseAsyncTemplateForAnyAwaitable"/> is <c>true</c>, otherwise <see cref="OverrideMethod"/> is used.
        /// </summary>
        [Template( IsEmpty = true )]
        public virtual IAsyncEnumerator<dynamic?> OverrideAsyncEnumeratorMethod() => throw new NotSupportedException();
#endif

        /// <summary>
        /// The default template for overriding method implementations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the primary template method that must be implemented in derived aspects. It is used for all methods where
        /// a more specific template (<see cref="OverrideAsyncMethod"/>, <see cref="OverrideEnumerableMethod"/>,
        /// <see cref="OverrideEnumeratorMethod"/>, <c>OverrideAsyncEnumerableMethod</c>, or <c>OverrideAsyncEnumeratorMethod</c>)
        /// does not apply or has not been overridden.
        /// </para>
        /// <para>
        /// Within the template, use <c>meta.Proceed()</c> to invoke the original method implementation, and <c>meta.Target</c>
        /// to access information about the target method.
        /// </para>
        /// </remarks>
        /// <returns>The return value of the method. Use <c>dynamic?</c> to support any return type.</returns>
        [Template]
        public abstract dynamic? OverrideMethod();
    }
}