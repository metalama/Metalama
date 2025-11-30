// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Advising
{
    /// <summary>
    /// Specifies which T# templates to use when overriding methods, enabling automatic selection of specialized
    /// templates based on the target method's characteristics (async, iterator, async iterator).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When overriding methods with <see cref="AdviserExtensions.Override(IAdviser{Code.IMethod}, in MethodTemplateSelector, object?, object?)"/>,
    /// different target methods may require different template implementations. For example, an async method might need
    /// a template that uses <c>await meta.ProceedAsync()</c>, while an iterator method might need <c>yield return</c> semantics.
    /// </para>
    /// <para>
    /// <see cref="MethodTemplateSelector"/> allows you to specify multiple templates and let Metalama automatically
    /// select the appropriate one. Template selection depends on two factors: the method's characteristics and the
    /// <see cref="UseAsyncTemplateForAnyAwaitable"/> and <see cref="UseEnumerableTemplateForAnyEnumerable"/> flags.
    /// </para>
    /// <para>
    /// <b>Default behavior</b> (both flags are <c>false</c>): Templates are selected based on how the method is <i>implemented</i>:
    /// <list type="bullet">
    /// <item><description><see cref="AsyncTemplate"/>: Methods with the <c>async</c> modifier</description></item>
    /// <item><description><see cref="EnumerableTemplate"/>/<see cref="EnumeratorTemplate"/>: Methods using <c>yield</c> statements</description></item>
    /// <item><description><see cref="AsyncEnumerableTemplate"/>/<see cref="AsyncEnumeratorTemplate"/>: Async iterator methods (both <c>async</c> and <c>yield</c>)</description></item>
    /// <item><description><see cref="DefaultTemplate"/>: All other methods (required)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>When <see cref="UseAsyncTemplateForAnyAwaitable"/> is <c>true</c></b>: <see cref="AsyncTemplate"/> is selected based on
    /// the method's <i>return type</i> being awaitable (e.g., <c>Task</c>, <c>ValueTask</c>), regardless of whether the method
    /// has the <c>async</c> modifier.
    /// </para>
    /// <para>
    /// <b>When <see cref="UseEnumerableTemplateForAnyEnumerable"/> is <c>true</c></b>: Iterator templates are selected based on
    /// the method's <i>return type</i> (e.g., <see cref="IEnumerable{T}"/>, <c>IAsyncEnumerable</c>), regardless of whether
    /// the method uses <c>yield</c> statements.
    /// </para>
    /// <para>
    /// This type has an implicit conversion from <see cref="string"/>, so if you only need a default template,
    /// you can pass the template name directly without constructing a <see cref="MethodTemplateSelector"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="GetterTemplateSelector"/>
    /// <seealso cref="AdviserExtensions.Override(IAdviser{Code.IMethod}, in MethodTemplateSelector, object?, object?)"/>
    /// <seealso cref="IAdviser{T}"/>
    /// <seealso cref="TemplateAttribute"/>
    /// <seealso cref="OverrideMethodAspect"/>
    /// <seealso href="@overriding-methods"/>
    /// <seealso href="@templates"/>
    [CompileTime]
    public readonly struct MethodTemplateSelector
    {
        /// <summary>
        /// Gets the name of the template that must be applied if no other template is applicable. This property is required.
        /// </summary>
        public string DefaultTemplate { get; }

        /// <summary>
        /// Gets the name of the template that must be applied to async methods, including async iterators (unless <see cref="AsyncEnumerableTemplate"/>
        /// or <see cref="AsyncEnumeratorTemplate"/> is defined).
        /// If <see cref="UseAsyncTemplateForAnyAwaitable"/> is set to <c>true</c>, this template will be used for any method that has an awaitable return type,
        /// including <c>IAsyncEnumerable</c> and
        /// <c>IAsyncEnumerator</c>.
        /// </summary>
        public string? AsyncTemplate { get; }

        /// <summary>
        /// Gets the name of the template that must be applied to yield-based iterator methods returning an <see cref="IEnumerable{T}"/> or <see cref="IEnumerable"/>.
        /// If the <see cref="UseEnumerableTemplateForAnyEnumerable"/> is set to <c>true</c>, this template will be used for
        /// any method that returns the <see cref="IEnumerable{T}"/> or <see cref="IEnumerable"/> type, even if it not a yield-based iterator. 
        /// </summary>
        public string? EnumerableTemplate { get; }

        /// <summary>
        /// Gets the name of the template that must be applied to yield-based iterator methods returning an <see cref="IEnumerator{T}"/> or <see cref="IEnumerator"/>.
        /// If the <see cref="UseEnumerableTemplateForAnyEnumerable"/> is set to <c>true</c>, this template will be used for
        /// any method that returns the <see cref="IEnumerator{T}"/> or <see cref="IEnumerator"/> type, even if it not a yield-based iterator. 
        /// </summary>
        public string? EnumeratorTemplate { get; }

        /// <summary>
        /// Gets the name of the template that must be applied to an async iterator method returning the <c>IAsyncEnumerable</c> type.
        /// If the <see cref="UseEnumerableTemplateForAnyEnumerable"/> is set to <c>true</c>, this template will be used for
        /// any method that returns the <c>IAsyncEnumerable</c> type, even if it is not implemented as an async or yield-based iterator.
        /// </summary>
        public string? AsyncEnumerableTemplate { get; }

        /// <summary>
        /// Gets the name of the template that must be applied to an async iterator method returning the <c>IAsyncEnumerable</c> type.
        /// If the <see cref="UseEnumerableTemplateForAnyEnumerable"/> is set to <c>true</c>, this template will be used for
        /// any method that returns the <c>IAsyncEnumerable</c> type, even if it is not implemented as an async or yield-based iterator.
        /// </summary>
        public string? AsyncEnumeratorTemplate { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="AsyncTemplate"/> must be applied to all methods returning an awaitable type (including <c>IAsyncEnumerable</c>
        /// and <c>IAsyncEnumerator</c>) instead of only to methods that have the <c>async</c> modifier. If the implementation of the template method is async,
        /// the awaitable type must also have an async method builder, otherwise the method will be processed by <see cref="DefaultTemplate"/>.
        /// </summary>
        public bool UseAsyncTemplateForAnyAwaitable { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="EnumerableTemplate"/>, <see cref="EnumeratorTemplate"/>, <see cref="AsyncEnumerableTemplate"/>,
        /// <see cref="AsyncEnumeratorTemplate"/> must be applied to all methods returning a compatible return type (if set to <c>true</c>),
        /// instead of only to methods using the <c>yield</c> statement.
        /// </summary>
        public bool UseEnumerableTemplateForAnyEnumerable { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodTemplateSelector"/> struct by specifying the name of the template methods to be applied. The named passed to this
        /// constructor must be the name of methods of the current aspect class, and these methods must be annotated with the <see cref="TemplateAttribute"/> custom attribute.
        /// You can define several templates by passing a value to optional parameters. The appropriate template will be automatically selected according to
        /// the method to which the advice is applied. If several templates are eligible for a method, the template that is the last in the list of parameters is selected.
        /// </summary>
        /// <param name="defaultTemplate">Name of the template that must be applied if no other template is applicable. This parameter is required.
        /// See <see cref="DefaultTemplate"/> for details.</param>
        /// <param name="asyncTemplate">Name of the template that must be applied to async methods, including async iterators unless specified otherwise.
        /// See <see cref="AsyncTemplate"/> for details.</param>
        /// <param name="enumerableTemplate">Name of the template that must be applied to iterator methods  returning an an <see cref="IEnumerable{T}"/> or <see cref="IEnumerable"/>.
        /// See <see cref="EnumerableTemplate"/> for details.</param>
        /// <param name="enumeratorTemplate">Name of the template that must be applied to an iterator method returning an an <see cref="IEnumerator{T}"/> or <see cref="IEnumerator"/>.
        /// See <see cref="EnumeratorTemplate"/> for details.</param>
        /// <param name="asyncEnumerableTemplate">Name of the template that must be applied to an async iterator method returning an <c>IAsyncEnumerable</c>.
        /// See <see cref="AsyncEnumerableTemplate"/> for details.
        /// </param>
        /// <param name="asyncEnumeratorTemplate">Name of the template that must be applied to an async iterator method returning an <c>IAsyncEnumerator</c>.
        /// See <see cref="AsyncEnumeratorTemplate"/> for details.</param>
        /// <param name="useAsyncTemplateForAnyAwaitable">Indicates whether the <see cref="AsyncTemplate"/> must be applied to all methods returning an awaitable
        /// type (including <c>IAsyncEnumerable</c> and <c>IAsyncEnumerator</c>), instead of only to methods that have the <c>async</c> modifier.
        /// See <see cref="UseAsyncTemplateForAnyAwaitable"/> for details.  
        /// </param>
        /// <param name="useEnumerableTemplateForAnyEnumerable">Indicates whether the <see cref="EnumerableTemplate"/>, <see cref="EnumeratorTemplate"/>, <see cref="AsyncEnumerableTemplate"/>,
        /// <see cref="AsyncEnumeratorTemplate"/> must be applied to all methods returning a compatible return type (if set to <c>true</c>),
        /// instead of only to methods using the <c>yield</c> statement.
        /// See <see cref="UseEnumerableTemplateForAnyEnumerable"/> for details.
        /// </param>
        /// <remarks>
        /// Note that this type has also an implicit conversion from <see cref="string"/>.
        /// If you only want to specify a default template, you can pass a string, without calling the constructor.
        /// </remarks>
        public MethodTemplateSelector(
            string defaultTemplate,
            string? asyncTemplate = null,
            string? enumerableTemplate = null,
            string? enumeratorTemplate = null,
            string? asyncEnumerableTemplate = null,
            string? asyncEnumeratorTemplate = null,
            bool useAsyncTemplateForAnyAwaitable = false,
            bool useEnumerableTemplateForAnyEnumerable = false )
        {
            this.DefaultTemplate = defaultTemplate;
            this.UseAsyncTemplateForAnyAwaitable = useAsyncTemplateForAnyAwaitable;
            this.UseEnumerableTemplateForAnyEnumerable = useEnumerableTemplateForAnyEnumerable;
            this.AsyncTemplate = asyncTemplate;
            this.EnumerableTemplate = enumerableTemplate;
            this.EnumeratorTemplate = enumeratorTemplate;
            this.AsyncEnumerableTemplate = asyncEnumerableTemplate;
            this.AsyncEnumeratorTemplate = asyncEnumeratorTemplate;
        }

        /// <summary>
        /// Converts a <see cref="string"/> to a new instance of the <see cref="MethodTemplateSelector"/> where the <see cref="DefaultTemplate"/> property is
        /// set to this string.
        /// </summary>
        /// <param name="defaultTemplate">Name of the default template.</param>
        /// <returns>A new <see cref="MethodTemplateSelector"/> with the specified default template.</returns>
        public static implicit operator MethodTemplateSelector( string defaultTemplate ) => new( defaultTemplate );

        internal GetterTemplateSelector AsGetterTemplateSelector()
            => new( this.DefaultTemplate, this.EnumerableTemplate, this.EnumeratorTemplate, this.UseEnumerableTemplateForAnyEnumerable );
    }
}