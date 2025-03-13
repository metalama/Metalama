// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Invokers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

internal sealed class IntroducedIndexer : IntroducedPropertyOrIndexer, IIndexerImpl
{
    private readonly IndexerBuilderData _indexerBuilderData;

    public IntroducedIndexer( IndexerBuilderData builderData, CompilationModel compilation, IGenericContext genericContext ) : base(
        compilation,
        genericContext )
    {
        this._indexerBuilderData = builderData;
    }

    public override DeclarationBuilderData BuilderData => this._indexerBuilderData;

    protected override NamedDeclarationBuilderData NamedDeclarationBuilderData => this._indexerBuilderData;

    protected override MemberOrNamedTypeBuilderData MemberOrNamedTypeBuilderData => this._indexerBuilderData;

    protected override MemberBuilderData MemberBuilderData => this._indexerBuilderData;

    public override bool IsExplicitInterfaceImplementation => this.ExplicitInterfaceImplementations.Count > 0;

    protected override PropertyOrIndexerBuilderData PropertyOrIndexerBuilderData => this._indexerBuilderData;

    [Memo]
    public IParameterList Parameters
        => new ParameterList(
            this,
            this.Compilation.GetParameterCollection( this.Ref ) );

    [Memo]
    public IIndexer? OverriddenIndexer => this.MapDeclaration( this._indexerBuilderData.OverriddenIndexer );

    [Memo]
    public IIndexer Definition => this.Compilation.Factory.GetIndexer( this._indexerBuilderData ).AssertNotNull();

    protected override IMemberOrNamedType GetDefinition() => this.Definition;

    [Memo]
    private IFullRef<IIndexer> Ref => this.RefFactory.FromIntroducedDeclaration<IIndexer>( this );

    public IRef<IIndexer> ToRef() => this.Ref;

    protected override IFullRef<IMember> ToMemberFullRef() => this.Ref;

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    public IIndexerInvoker With( InvokerOptions options ) => new IndexerInvoker( this, options );

    public IIndexerInvoker With( object? target, InvokerOptions options = default ) => new IndexerInvoker( this, options, target );

    public object GetValue( params object?[] args ) => new IndexerInvoker( this ).GetValue( args );

    public object SetValue( object? value, params object?[] args ) => new IndexerInvoker( this ).SetValue( value, args );

    // TODO: When an interface is introduced, explicit implementation should appear here.
    [Memo]
    public IReadOnlyList<IIndexer> ExplicitInterfaceImplementations
        => this._indexerBuilderData.ExplicitInterfaceImplementations.SelectAsImmutableArray( this.MapDeclaration );
}