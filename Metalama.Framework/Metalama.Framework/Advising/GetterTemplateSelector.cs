// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Advising
{
    /// <summary>
    /// Specifies the templates that must be used for the <c>get</c> accessor by the <c>IAdviceFactory.OverrideAccessors</c> advice.
    /// </summary>
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
        /// <returns></returns>
        public static implicit operator GetterTemplateSelector( string? defaultTemplate ) => new( defaultTemplate );
    }
}