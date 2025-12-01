// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Advising
{
    /// <summary>
    /// Specifies which T# templates to use when overriding property or field getters, enabling automatic selection
    /// of specialized templates for iterator getters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When overriding field or property accessors with <see cref="AdviserExtensions.OverrideAccessors(IAdviser{Code.IFieldOrProperty}, in GetterTemplateSelector, string?, object?, object?)"/>,
    /// you can provide different templates for properties that return <see cref="IEnumerable{T}"/> or <see cref="IEnumerator{T}"/>.
    /// Template selection depends on the <see cref="UseEnumerableTemplateForAnyEnumerable"/> flag.
    /// </para>
    /// <para>
    /// <b>Default behavior</b> (<see cref="UseEnumerableTemplateForAnyEnumerable"/> is <c>false</c>): Templates are selected based
    /// on how the getter is <i>implemented</i>:
    /// <list type="bullet">
    /// <item><description><see cref="EnumerableTemplate"/>/<see cref="EnumeratorTemplate"/>: Getters using <c>yield</c> statements</description></item>
    /// <item><description><see cref="DefaultTemplate"/>: All other getters (required if you want to override)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>When <see cref="UseEnumerableTemplateForAnyEnumerable"/> is <c>true</c></b>: Iterator templates are selected based on
    /// the property's <i>return type</i> (e.g., <see cref="IEnumerable{T}"/>, <see cref="IEnumerator{T}"/>), regardless of whether
    /// the getter uses <c>yield</c> statements.
    /// </para>
    /// <para>
    /// This type has an implicit conversion from <see cref="string"/>, so if you only need a default template,
    /// you can pass the template name directly without constructing a <see cref="GetterTemplateSelector"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="AdviserExtensions.OverrideAccessors(IAdviser{Code.IFieldOrProperty}, in GetterTemplateSelector, string?, object?, object?)"/>
    /// <seealso cref="MethodTemplateSelector"/>
    /// <seealso cref="TemplateAttribute"/>
    /// <seealso cref="OverrideFieldOrPropertyAspect"/>
    /// <seealso href="@overriding-fields-or-properties"/>
    /// <seealso href="@templates"/>
    [CompileTime]
    [PublicAPI]
    public readonly struct GetterTemplateSelector
    {
        /// <summary>
        /// Gets the name of the template that must be applied if no other template is applicable. This property is required if you want to
        /// override the getter.
        /// </summary>
        public string? DefaultTemplate { get; }

        /// <summary>
        /// Gets the name of the template that must be applied to iterator getters returning an <see cref="IEnumerable{T}"/> or <see cref="IEnumerable"/>.
        /// </summary>
        public string? EnumerableTemplate { get; }

        /// <summary>
        /// Gets the name of the template that must be applied to iterator getters returning an <see cref="IEnumerator{T}"/> or <see cref="IEnumerator"/>.
        /// </summary>
        public string? EnumeratorTemplate { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="EnumerableTemplate"/> or <see cref="EnumeratorTemplate"/>
        /// must be applied to all methods returning compatible return type, instead of only to methods using the <c>yield</c> statement.
        /// </summary>
        public bool UseEnumerableTemplateForAnyEnumerable { get; }

        internal bool HasOnlyDefaultTemplate => this.EnumeratorTemplate == null && this.EnumeratorTemplate == null;

        internal bool IsNull => this.DefaultTemplate == null;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetterTemplateSelector"/> struct by specifying the name of the template methods to be applied. The named passed to this
        /// constructor must be the name of methods of the current aspect class, and these methods must be annotated with the <see cref="TemplateAttribute"/> custom attribute.
        /// You can define several templates by passing a value to optional parameters. The appropriate template will be automatically selected according to
        /// the method to which the advice is applied. If several templates are eligible for a method, the template that is the last in the list of parameters is selected.
        /// </summary>
        /// <param name="defaultTemplate">Name of the template that must be applied if no other template is applicable. This parameter is required.</param>
        /// <param name="enumerableTemplate">Name of the template that must be applied to iterator methods returning an <see cref="IEnumerable{T}"/> or <see cref="IEnumerable"/>.
        /// See <see cref="EnumerableTemplate"/> for details.</param>
        /// <param name="enumeratorTemplate">Name of the template that must be applied to an iterator method returning an an <see cref="IEnumerator{T}"/> or <see cref="IEnumerator"/>.
        /// See <see cref="EnumeratorTemplate"/> for details.</param>
        /// <param name="useEnumerableTemplateForAnyEnumerable">Indicates whether the <see cref="EnumerableTemplate"/> or <see cref="EnumeratorTemplate"/>
        /// must be applied to all methods returning compatible return type (if set to <c>true</c>), instead of only to methods using the <c>yield</c> statement.
        /// See <see cref="UseEnumerableTemplateForAnyEnumerable"/> for details.</param>
        /// <remarks>
        /// Note that this type has also an implicit conversion from <see cref="string"/>.
        /// If you only want to specify a default template, you can pass a string, without calling the constructor.
        /// </remarks>
        public GetterTemplateSelector(
            string defaultTemplate,
            string? enumerableTemplate = null,
            string? enumeratorTemplate = null,
            bool useEnumerableTemplateForAnyEnumerable = false )
        {
            this.DefaultTemplate = defaultTemplate;
            this.UseEnumerableTemplateForAnyEnumerable = useEnumerableTemplateForAnyEnumerable;
            this.EnumerableTemplate = enumerableTemplate;
            this.EnumeratorTemplate = enumeratorTemplate;
        }

        private GetterTemplateSelector( string? defaultTemplate )
        {
            this.DefaultTemplate = defaultTemplate;
            this.UseEnumerableTemplateForAnyEnumerable = false;
            this.EnumerableTemplate = null;
            this.EnumeratorTemplate = null;
        }

        /// <summary>
        /// Converts a <see cref="string"/> to a new instance of the <see cref="GetterTemplateSelector"/> where the <see cref="DefaultTemplate"/> property is
        /// set to this string.
        /// </summary>
        /// <param name="defaultTemplate">Name of the default template.</param>
        /// <returns>A new <see cref="GetterTemplateSelector"/> with the specified default template.</returns>
        public static implicit operator GetterTemplateSelector( string? defaultTemplate ) => new( defaultTemplate );
    }
}