// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.SerializableIds;

namespace Metalama.Framework.Engine.CompileTime
{
    internal sealed class TemplateInfo
    {
        /// <summary>
        /// Gets a magic value representing the fact that the member is not a template.
        /// </summary>
        public static TemplateInfo None { get; } = new();

        public bool IsNone => this.AttributeType == TemplateAttributeType.None;

        /// <summary>
        /// Gets a value indicating whether the template member is abstract.
        /// </summary>
        public bool IsAbstract { get; }

        /// <summary>
        /// Gets a value indicating whether the template member does not have a body (introductions of partial and abstract methods).
        /// </summary>
        public bool HasNoBody { get; }

        /// <summary>
        /// Gets a value indicating whether the template member can be referenced from a template as run-time code,
        /// which is typically the case with introductions.
        /// </summary>
        public bool CanBeReferencedAsRunTimeCode => this.AttributeType is TemplateAttributeType.DeclarativeAdvice or TemplateAttributeType.InterfaceMember;

        public bool CanBeReferencedAsSubtemplate => !this.IsNone && !this.CanBeReferencedAsRunTimeCode;

        /// <summary>
        /// Gets the <see cref="TemplateAttribute"/> if it could be instantiated by the <see cref="SymbolClassifier"/>, i.e.
        /// only if it is a system attribute but not if it is defined in user code.
        /// </summary>
        private IAdviceAttribute? Attribute { get; }

        public TemplateAttributeType AttributeType { get; }

        /// <summary>
        /// Gets the <see cref="SymbolId"/> of the template member.
        /// </summary>
        private SymbolId SymbolId { get; }

        public TemplateInfo( SymbolId symbolId, TemplateAttributeType attributeType, IAdviceAttribute? attribute )
        {
            this.AttributeType = attributeType;
            this.Attribute = attribute;
            this.SymbolId = symbolId;
        }

        private TemplateInfo()
        {
            this.Attribute = TemplateAttribute.Default;
            this.AttributeType = TemplateAttributeType.None;
        }

        private TemplateInfo( TemplateInfo prototype, bool isAbstract, bool hasNoBody )
        {
            this.Attribute = prototype.Attribute;
            this.AttributeType = prototype.AttributeType;
            this.SymbolId = prototype.SymbolId;
            this.IsAbstract = isAbstract;
            this.HasNoBody = hasNoBody;
        }

        public TemplateInfo( TemplateAttributeType attributeType, bool isAbstract )
        {
            this.AttributeType = attributeType;
            this.IsAbstract = isAbstract;
        }

        /// <summary>
        /// Returns a copy of the current <see cref="TemplateInfo"/>, but with the <see cref="IsAbstract"/> property set to <c>true</c>.
        /// </summary>
        public TemplateInfo AsAbstract() => new( this, true, false );

        public TemplateInfo WithoutBody() => new( this, false, true );

        public override string ToString() => $"Type={this.AttributeType}, Attribute={this.Attribute}";
    }
}