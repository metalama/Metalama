// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
using System;
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
        => this.PropertySymbol.Equals( this.PropertySymbol.OriginalDefinition )
            ? this
            : this.Compilation.Factory.GetIndexer( this.PropertySymbol.OriginalDefinition );

    protected override IMemberOrNamedType GetDefinitionMemberOrNamedType() => this.Definition;

    [Memo]
    private IIndexerInvoker Invoker => new IndexerInvoker( this );

    IIndexerInvoker IIndexerInvoker.WithOptions( InvokerOptions options ) => this.Invoker.WithOptions( options );

    IIndexerInvoker IIndexerInvoker.WithObject( object obj ) => this.Invoker.WithObject( obj );

    IIndexerInvoker IIndexerInvoker.WithObject( IExpression obj ) => this.Invoker.WithObject( obj );

    IIndexerInvoker IIndexerInvoker.With( InvokerOptions options ) => this.Invoker.WithOptions( options );

    IIndexerInvoker IIndexerInvoker.With( object? target, InvokerOptions options ) => this.Invoker.WithOptions( options ).WithObject( target! );

    IExpression IIndexerInvoker.this[ params IExpression[] args ] => this.Invoker[args];

    IExpression IIndexerInvoker.this[ params object?[] args ] => this.Invoker[args];

    [Obsolete]
    object? IIndexerInvoker.GetValue( params object?[] args ) => this.Invoker.GetValue( args );

    [Obsolete]
    object? IIndexerInvoker.SetValue( object? value, params object?[] args ) => this.Invoker.SetValue( value, args );

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