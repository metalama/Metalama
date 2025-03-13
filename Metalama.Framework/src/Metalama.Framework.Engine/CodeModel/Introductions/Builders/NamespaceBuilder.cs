// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.Collections;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal sealed class NamespaceBuilder : NamedDeclarationBuilder, INamespace
{
    private string _name;

    public IntroducedRef<INamespace> Ref { get; }

    public NamespaceBuilder( AspectLayerInstance aspectLayerInstance, INamespace containingNamespace, string name ) : base( aspectLayerInstance )
    {   
        Invariant.Assert( !name.ContainsOrdinal( '.' ) );
        
        this._name = name;
        this.ContainingNamespace = containingNamespace;
        this.Ref = new IntroducedRef<INamespace>( this.Compilation.RefFactory );
        this.Freeze();
    }

    public override string Name
    {
        get => this._name;
        set
        {
            this.CheckNotFrozen();
            this._name = value;
        }
    }

    public string FullName
        => !this.ContainingNamespace.AssertNotNull().IsGlobalNamespace
            ? $"{this.ContainingNamespace.FullName}.{this.Name}"
            : this.Name;

    public bool IsGlobalNamespace => false;

    public INamespace? ContainingNamespace { get; }

    INamespace? INamespace.ParentNamespace => this.ContainingNamespace;

    [Memo]
    public INamedTypeCollection Types => new EmptyNamedTypeCollection();

    [Memo]
    public INamespaceCollection Namespaces => new EmptyNamespaceCollection();

    public bool IsPartial => false;

    public override bool IsDesignTimeObservable => true;

    public override IDeclaration? ContainingDeclaration => this.ContainingNamespace;

    public override DeclarationKind DeclarationKind => DeclarationKind.Namespace;

    public override bool CanBeInherited => false;

    public override SyntaxTree? PrimarySyntaxTree => null;

    public INamespace? GetDescendant( string ns )
    {
        // TODO: Implement this.
        return null;
    }

    IRef<INamespace> INamespace.ToRef() => this.Ref;

    protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    IRef<INamespaceOrNamedType> INamespaceOrNamedType.ToRef() => this.Ref;

    protected override void EnsureReferenceInitialized()
    {
        this.Ref.BuilderData = new NamespaceBuilderData( this, this.ContainingDeclaration.AssertNotNull().ToFullRef() );
    }

    public NamespaceBuilderData BuilderData => (NamespaceBuilderData) this.Ref.BuilderData;
}