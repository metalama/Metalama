// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Invokers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Source;

internal sealed class SourceIndexer : SourcePropertyOrIndexer, IIndexerImpl
{
    public SourceIndexer( IPropertySymbol symbol, CompilationModel compilation, GenericContext? genericContextForSymbolMapping ) : base(
        symbol,
        compilation,
        genericContextForSymbolMapping ) { }

    [Memo]
    public IParameterList Parameters
        => new ParameterList(
            this,
            this.PropertySymbol.Parameters.Select( p => this.RefFactory.FromSymbol<IParameter>( p, this.GenericContextForSymbolMapping ) ).ToReadOnlyList() );

    public IIndexer? OverriddenIndexer
    {
        get
        {
            var overriddenProperty = this.PropertySymbol.OverriddenProperty;

            if ( overriddenProperty != null )
            {
                return this.Compilation.Factory.GetIndexer( overriddenProperty, this.GenericContextForSymbolMapping );
            }
            else
            {
                return null;
            }
        }
    }

    [Memo]
    public IIndexer Definition
        => this.PropertySymbol == this.PropertySymbol.OriginalDefinition ? this : this.Compilation.Factory.GetIndexer( this.PropertySymbol.OriginalDefinition );

    protected override IMemberOrNamedType GetDefinitionMemberOrNamedType() => this.Definition;

    public IIndexerInvoker With( InvokerOptions options ) => new IndexerInvoker( this, options );

    public IIndexerInvoker With( object? target, InvokerOptions options = default ) => new IndexerInvoker( this, options, target );

    public object GetValue( params object?[] args ) => new IndexerInvoker( this ).GetValue( args );

    public object SetValue( object? value, params object?[] args ) => new IndexerInvoker( this ).SetValue( value, args );

    public override IMember? OverriddenMember => this.OverriddenIndexer;

    [Memo]
    public IReadOnlyList<IIndexer> ExplicitInterfaceImplementations
        => this.PropertySymbol.ExplicitInterfaceImplementations.Select( p => this.Compilation.Factory.GetIndexer( p ) ).ToReadOnlyList();

    public override DeclarationKind DeclarationKind => DeclarationKind.Indexer;

    [Memo]
    private IFullRef<IIndexer> Ref => this.RefFactory.FromSymbol<IIndexer>( this.PropertySymbol, this.GenericContextForSymbolMapping );

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    IRef<IIndexer> IIndexer.ToRef() => this.Ref;

    protected override IRef<IPropertyOrIndexer> ToPropertyOrIndexerRef() => this.Ref;

    protected override IRef<IFieldOrPropertyOrIndexer> ToFieldOrPropertyOrIndexerRef() => this.Ref;

    protected override IRef<IMember> ToMemberRef() => this.Ref;

    protected override IRef<IMemberOrNamedType> ToMemberOrNamedTypeRef() => this.Ref;
}