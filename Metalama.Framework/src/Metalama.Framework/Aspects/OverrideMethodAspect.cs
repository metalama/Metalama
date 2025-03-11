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

        [Template( IsEmpty = true )]
        public virtual Task<dynamic?> OverrideAsyncMethod() => throw new NotSupportedException();

        [Template( IsEmpty = true )]
        public virtual IEnumerable<dynamic?> OverrideEnumerableMethod() => throw new NotSupportedException();

        [Template( IsEmpty = true )]
        public virtual IEnumerator<dynamic?> OverrideEnumeratorMethod() => throw new NotSupportedException();

#if NET5_0_OR_GREATER
        [Template( IsEmpty = true )]
        public virtual IAsyncEnumerable<dynamic?> OverrideAsyncEnumerableMethod() => throw new NotSupportedException();

        [Template( IsEmpty = true )]
        public virtual IAsyncEnumerator<dynamic?> OverrideAsyncEnumeratorMethod() => throw new NotSupportedException();
#endif

        /// <summary>
        /// Default template of the new method implementation.
        /// </summary>
        /// <returns></returns>
        [Template]
        public abstract dynamic? OverrideMethod();
    }
}