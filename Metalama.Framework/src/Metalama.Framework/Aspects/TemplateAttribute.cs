// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// The base class for all custom attributes that mark a declaration as a template.
    /// </summary>
    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event )]
    public class TemplateAttribute : Attribute, ITemplateAttribute
    {
        private TemplateAttributeProperties _properties = new();

        internal static TemplateAttribute Default { get; } = new();

        public string? Name
        {
            get => this._properties.Name;
            set => this._properties = this._properties with { Name = value };
        }

        public Accessibility Accessibility
        {
            get => this._properties.Accessibility.GetValueOrDefault();
            set => this._properties = this._properties with { Accessibility = value };
        }

        public bool IsVirtual
        {
            get => this._properties.IsVirtual.GetValueOrDefault();
            set => this._properties = this._properties with { IsVirtual = value };
        }

        public bool IsSealed
        {
            get => this._properties.IsSealed.GetValueOrDefault();
            set => this._properties = this._properties with { IsSealed = value };
        }

        public bool IsRequired
        {
            get => this._properties.IsRequired.GetValueOrDefault();
            set => this._properties = this._properties with { IsRequired = value };
        }

        public bool IsAbstract
        {
            get => this._properties.IsAbstract.GetValueOrDefault();
            set => this._properties = this._properties with { IsAbstract = value };
        }

        public bool IsPartial
        {
            get => this._properties.IsPartial.GetValueOrDefault();
            set => this._properties = this._properties with { IsPartial = value };
        }

        public bool IsExtern
        {
            get => this._properties.IsExtern.GetValueOrDefault();
            set => this._properties = this._properties with { IsExtern = value };
        }

        /// <summary>
        /// Gets or sets a value indicating whether the template is an empty implementation, which means that the framework will consider the template
        /// to be undefined unless it is overridden in a derived class. It is similar to an abstract template implementation, but aspect deriving
        /// from the abstract class is not obliged to provide an implementation for the empty but non-abstract template.
        /// </summary>
        public bool IsEmpty { get; set; }

        TemplateAttributeProperties ITemplateAttribute.Properties => this._properties;
    }
}