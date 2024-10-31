// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

internal sealed class IntroducedNamespace : IntroducedNamedDeclaration, INamespace
{
    private readonly NamespaceBuilderData _namedDeclarationBuilderData;

    public IntroducedNamespace( NamespaceBuilderData builderData, CompilationModel compilation ) : base( compilation, GenericContext.Empty )
    {
        this._namedDeclarationBuilderData = builderData;
    }

    public override DeclarationBuilderData BuilderData => this.NamedDeclarationBuilderData;

    protected override NamedDeclarationBuilderData NamedDeclarationBuilderData => this._namedDeclarationBuilderData;

    [Memo]
    public string FullName => this.ContainingNamespace.IsGlobalNamespace ? this.Name : this.ContainingNamespace.FullName + "." + this.Name;

    public bool IsGlobalNamespace => false;

    [Memo]
    public INamespace ContainingNamespace => this.MapDeclaration( this.NamedDeclarationBuilderData.ContainingDeclaration.As<INamespace>() );

    [Memo]
    private IFullRef<INamespace> Ref => this.RefFactory.FromIntroducedDeclaration<INamespace>( this );

    IRef<INamespace> INamespace.ToRef() => this.Ref;

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    IRef<INamespaceOrNamedType> INamespaceOrNamedType.ToRef() => this.Ref;

    INamespace INamespace.ParentNamespace => this.ContainingNamespace;

    public INamedTypeCollection Types
        => new NamedTypeCollection(
            this,
            this.Compilation.GetNamedTypeCollectionByParent( this._namedDeclarationBuilderData.ToRef() ) );

    public INamespaceCollection Namespaces
        => new NamespaceCollection(
            this,
            this.Compilation.GetNamespaceCollection( this.NamedDeclarationBuilderData.ToFullRef().As<INamespace>() ) );

    public bool IsPartial => false;

    public INamespace GetDescendant( string ns ) => throw new NotImplementedException();

    public override bool CanBeInherited => false;

    public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = DerivedTypesOptions.Default )
        => throw new NotSupportedException();
}