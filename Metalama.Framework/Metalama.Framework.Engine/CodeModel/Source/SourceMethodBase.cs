// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System.Reflection;
using MethodKind = Metalama.Framework.Code.MethodKind;
using SymbolMethodKind = Microsoft.CodeAnalysis.MethodKind;

namespace Metalama.Framework.Engine.CodeModel.Source
{
    internal abstract class SourceMethodBase : SourceMember, IMethodBase
    {
        public override ISymbol Symbol => this.MethodSymbol;

        protected IMethodSymbol MethodSymbol { get; }

        [Memo]
        public override IDeclaration? ContainingDeclaration
            => this.Symbol switch
            {
                IMethodSymbol
                {
                    MethodKind: SymbolMethodKind.PropertyGet or SymbolMethodKind.PropertySet or SymbolMethodKind.EventAdd or SymbolMethodKind.EventRemove
                    or SymbolMethodKind.EventRaise
                } method => this.Compilation.Factory.GetDeclaration( method.AssociatedSymbol.AssertSymbolNotNull() ),
                _ => base.ContainingDeclaration
            };

        protected SourceMethodBase( IMethodSymbol symbol, CompilationModel compilation, GenericContext? genericContextForSymbolMapping ) : base(
            compilation,
            genericContextForSymbolMapping )
        {
            this.MethodSymbol = symbol.AssertBelongsToCompilationContext( compilation.CompilationContext );
        }

        [Memo]
        public IParameterList Parameters
            => new ParameterList(
                this,
                this.Compilation.GetParameterCollection( this.GetMethodBaseRef().DefinitionRef ) );

        public MethodKind MethodKind => this.MethodSymbol.MethodKind.ToOurMethodKind();

        public abstract MethodBase ToMethodBase();

        public IRef<IMethodBase> ToRef() => this.GetMethodBaseRef();

        protected abstract IFullRef<IMethodBase> GetMethodBaseRef();

        public override MemberInfo ToMemberInfo() => this.ToMethodBase();
    }
}