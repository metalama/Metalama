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
    /// A base aspect that overrides the implementation of a method.
    /// </summary>
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
        /// Gets or sets a value indicating whether the <see cref="OverrideAsyncMethod"/> template must be applied to all methods returning an awaitable
        /// type (including <c>IAsyncEnumerable</c> and <c>IAsyncEnumerator</c>), instead of only to methods that have the <c>async</c> modifier.
        /// </summary>
        protected bool UseEnumerableTemplateForAnyEnumerable { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="OverrideEnumerableMethod"/>, <see cref="OverrideEnumeratorMethod"/>,
        /// <c>OverrideAsyncEnumerableMethod"</c> or  <c>OverrideAsyncEnumeratorMethod"</c> template must be applied to all methods returning
        /// a compatible return type, instead of only to methods using the <c>yield</c> statement.
        /// </summary>
        protected bool UseAsyncTemplateForAnyAwaitable { get; init; }
#pragma warning restore SA1623

        public override void BuildEligibility( IEligibilityBuilder<IMethod> builder )
        {
            builder.AddRule( EligibilityRuleFactory.OverrideMethodAdviceRule );
        }

        /// <summary>
        /// Template of the new method implementation for asynchronous methods. By default, this template is used for methods with the <c>async</c> modifier.
        /// When <see cref="UseAsyncTemplateForAnyAwaitable"/> is <c>true</c>, this template is used for all methods returning an awaitable type
        /// (including <c>Task</c>, <c>ValueTask</c>, <c>IAsyncEnumerable</c>, and <c>IAsyncEnumerator</c>).
        /// If this template is not overridden, <see cref="OverrideMethod"/> is used instead.
        /// </summary>
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
        /// Default template of the new method implementation. This template is used for all methods where a more specific template
        /// (<see cref="OverrideAsyncMethod"/>, <see cref="OverrideEnumerableMethod"/>, <see cref="OverrideEnumeratorMethod"/>,
        /// <c>OverrideAsyncEnumerableMethod</c>, or <c>OverrideAsyncEnumeratorMethod</c>) has not been overridden or does not apply.
        /// </summary>
        [Template]
        public abstract dynamic? OverrideMethod();
    }
}