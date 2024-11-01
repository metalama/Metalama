// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;

namespace Metalama.Framework.Engine.CodeModel.Source
{
    internal abstract class SourceDeclaration : SymbolBasedDeclaration
    {
        protected SourceDeclaration( CompilationModel compilation, GenericContext? genericContextForSymbolMapping ) : base( genericContextForSymbolMapping )
        {
            this.Compilation = compilation;
        }

        protected virtual void OnUsingDeclaration() { }

        public override CompilationModel Compilation { get; }

        public override IAttributeCollection Attributes
        {
            get
            {
                this.OnUsingDeclaration();

                return this.AttributesImpl;
            }
        }

        [Memo]
        private IAttributeCollection AttributesImpl
            => new AttributeCollection(
                this,
                this.Compilation.GetAttributeCollection( this.GetDefinitionDeclaration().ToFullRef() ) );

        protected virtual IDeclaration GetDefinitionDeclaration() => this;

        [Memo]
        public override IAssembly? DeclaringAssembly
            => this.Symbol.ContainingAssembly is { } containingAssembly ? this.Compilation.Factory.GetAssembly( containingAssembly ) : null;

        public override string ToString()
        {
            this.OnUsingDeclaration();

            return base.ToString();
        }
    }
}