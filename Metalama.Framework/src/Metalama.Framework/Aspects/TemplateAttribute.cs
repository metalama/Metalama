// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Marks a method, property, field, or event as a T# template that can be used to generate or transform code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Templates are methods written in T#, a template language that is fully compatible with C# but has different semantics.
    /// T# combines compile-time logic with run-time code generation: templates execute at compile-time to produce the C# code
    /// that will run in your application. The T# compiler analyzes your code and separates compile-time portions from run-time
    /// portions, typically using the <see cref="meta"/> pseudo-keyword to identify compile-time expressions and statements.
    /// </para>
    /// <para>
    /// This attribute marks a member as a template. When used with <see cref="IntroduceAttribute"/> for declarative member
    /// introduction, or with advising methods like <see cref="AdviserExtensions.IntroduceMethod"/>, the properties of this
    /// attribute (such as <see cref="Name"/>, <see cref="Accessibility"/>, <see cref="IsVirtual"/>) control the characteristics
    /// of the introduced member. When a property is not explicitly set, its value defaults to the corresponding characteristic
    /// of the template member itself.
    /// </para>
    /// <para>
    /// Within templates, you can use <c>meta.Target</c> to access the declaration being overridden or introduced, and
    /// <c>meta.Proceed()</c> to invoke the original implementation when overriding. T# supports compile-time control flow
    /// (compile-time <c>if</c> and <c>foreach</c>) and dynamic typing through the <c>dynamic</c> keyword, which represents
    /// values whose types are unknown at template authoring time but are resolved when the template is applied to a specific
    /// target.
    /// </para>
    /// </remarks>
    /// <seealso cref="IntroduceAttribute"/>
    /// <seealso cref="ITemplateAttribute"/>
    /// <seealso cref="MethodTemplateSelector"/>
    /// <seealso cref="GetterTemplateSelector"/>
    /// <seealso cref="ITemplateProvider"/>
    /// <seealso href="@template-overview"/>
    /// <seealso href="@templates"/>
    /// <seealso href="@template-compile-time"/>
    /// <seealso href="@auxiliary-templates"/>
    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event )]
    public class TemplateAttribute : Attribute, ITemplateAttribute
    {
        private TemplateAttributeProperties _properties = new();

        internal static TemplateAttribute Default { get; } = new();

        /// <summary>
        /// Gets or sets the name of the member introduced by this template.
        /// </summary>
        /// <remarks>
        /// When this property is not set, the introduced member uses the same name as the template member.
        /// This property is only relevant when the template is used for member introduction.
        /// </remarks>
        public string? Name
        {
            get => this._properties.Name;
            set => this._properties = this._properties with { Name = value };
        }

        /// <summary>
        /// Gets or sets the accessibility of the introduced member. When this property is not set,
        /// the accessibility is taken from the template member that this attribute is applied to.
        /// </summary>
        public Accessibility Accessibility
        {
            get => this._properties.Accessibility.GetValueOrDefault();
            set => this._properties = this._properties with { Accessibility = value };
        }

        /// <summary>
        /// Gets or sets a value indicating whether the introduced member is virtual. When this property is not set,
        /// the value is taken from the template member that this attribute is applied to.
        /// </summary>
        public bool IsVirtual
        {
            get => this._properties.IsVirtual.GetValueOrDefault();
            set => this._properties = this._properties with { IsVirtual = value };
        }

        /// <summary>
        /// Gets or sets a value indicating whether the introduced member is sealed. When this property is not set,
        /// the value is taken from the template member that this attribute is applied to.
        /// </summary>
        public bool IsSealed
        {
            get => this._properties.IsSealed.GetValueOrDefault();
            set => this._properties = this._properties with { IsSealed = value };
        }

        /// <summary>
        /// Gets or sets a value indicating whether the introduced member is required. When this property is not set,
        /// the value is taken from the template member that this attribute is applied to.
        /// </summary>
        public bool IsRequired
        {
            get => this._properties.IsRequired.GetValueOrDefault();
            set => this._properties = this._properties with { IsRequired = value };
        }

        /// <summary>
        /// Gets or sets a value indicating whether the introduced member is abstract. When this property is not set,
        /// the value is taken from the template member that this attribute is applied to.
        /// </summary>
        public bool IsAbstract
        {
            get => this._properties.IsAbstract.GetValueOrDefault();
            set => this._properties = this._properties with { IsAbstract = value };
        }

        /// <summary>
        /// Gets or sets a value indicating whether the introduced member is partial. When this property is not set,
        /// the value is taken from the template member that this attribute is applied to.
        /// </summary>
        public bool IsPartial
        {
            get => this._properties.IsPartial.GetValueOrDefault();
            set => this._properties = this._properties with { IsPartial = value };
        }

        /// <summary>
        /// Gets or sets a value indicating whether the introduced member is extern. When this property is not set,
        /// the value is taken from the template member that this attribute is applied to.
        /// </summary>
        public bool IsExtern
        {
            get => this._properties.IsExtern.GetValueOrDefault();
            set => this._properties = this._properties with { IsExtern = value };
        }

        /// <summary>
        /// Gets or sets a value indicating whether this template is an empty placeholder implementation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When set to <c>true</c>, this template is treated as undefined unless overridden in a derived aspect class.
        /// This is similar to an abstract template, but unlike abstract templates, derived aspects are not required to
        /// provide an implementation.
        /// </para>
        /// <para>
        /// Empty templates are useful when creating aspect base classes that provide default no-op implementations for
        /// optional template methods (e.g., async or iterator method templates in <see cref="OverrideMethodAspect"/>).
        /// </para>
        /// </remarks>
        public bool IsEmpty { get; set; }

        TemplateAttributeProperties ITemplateAttribute.Properties => this._properties;
    }
}