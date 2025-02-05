// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Visitors;
using System.Linq;

namespace Metalama.Framework.Engine.Advising
{
    internal static class TemplateTypeRewriter
    {
        public static TypeRewriter Unbound => UnboundRewriter.Instance;

        public static TypeRewriter Get( PartiallyBoundTemplateMethod template )
            => template.TemplateMember.TemplateClassMember.TypeParameters.All( x => !x.IsCompileTime )
                ? UnboundRewriter.Instance
                : new TemplateBoundRewriter( template );

        private class UnboundRewriter : TypeRewriter
        {
            public static UnboundRewriter Instance { get; } = new();

            internal override IType Visit( IDynamicType dynamicType )
            {
                var objectType = dynamicType.GetCompilationModel().Cache.SystemObjectType;

                return dynamicType.IsNullable switch
                {
                    null => objectType,
                    true => objectType.ToNullable(),
                    false => objectType.ToNonNullable()
                };
            }
        }

        private sealed class TemplateBoundRewriter : UnboundRewriter
        {
            private readonly PartiallyBoundTemplateMethod _template;

            public TemplateBoundRewriter( PartiallyBoundTemplateMethod template )
            {
                this._template = template;
            }

            internal override IType Visit( ITypeParameter typeParameter )
            {
                if ( this._template.TemplateMember.TemplateClassMember.IndexedParameters.TryGetValue( typeParameter.Name, out var templateParameter )
                     && templateParameter.IsCompileTime )
                {
                    var index = templateParameter.TemplateIndex!.Value - this._template.TemplateMember.TemplateClassMember.Parameters.Length;
                    var factory = (TemplateTypeArgumentFactory) this._template.TypeArguments[index]!;

                    return (ITypeImpl) factory.Type;
                }
                else
                {
                    return typeParameter;
                }
            }
        }
    }
}