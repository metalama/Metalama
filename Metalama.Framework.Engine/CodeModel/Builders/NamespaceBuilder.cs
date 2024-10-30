// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.Builders;

internal sealed class NamespaceBuilder : NamedDeclarationBuilder, INamespace
{
    public string FullName
        => !this.ContainingNamespace.AssertNotNull().IsGlobalNamespace
            ? $"{this.ContainingNamespace.FullName}.{this.Name}"
            : this.Name;

    public bool IsGlobalNamespace => false;

    public INamespace? ContainingNamespace { get; }

    public IRef<INamespaceOrNamedType> ToRef() => this.BoxedRef;

    INamespace? INamespace.ParentNamespace => this.ContainingNamespace;

    [Memo]
    public INamedTypeCollection Types => new EmptyNamedTypeCollection();

    [Memo]
    public INamespaceCollection Namespaces => new EmptyNamespaceCollection();

    public bool IsPartial => false;

    public override IDeclaration? ContainingDeclaration => this.ContainingNamespace;

    public override DeclarationKind DeclarationKind => DeclarationKind.Namespace;

    public override bool CanBeInherited => false;

    public IntroduceNamespaceTransformation ToTransformation() => new( this.ParentAdvice, this );

    public NamespaceBuilder( Advice advice, INamespace containingNamespace, string name ) : base( advice, name )
    {
        Invariant.Assert( !name.ContainsOrdinal( '.' ) );

        this.ContainingNamespace = containingNamespace;
    }

    public override SyntaxTree? PrimarySyntaxTree => null;

    public INamespace? GetDescendant( string ns )
    {
        // TODO: Implement this.
        return null;
    }

    public override string ToDisplayString( CodeDisplayFormat? format = null, CodeDisplayContext? context = null ) => this.FullName;

    [Memo]
    public BoxedRef<INamespace> BoxedRef => new( this.ToValueTypedRef() );

    public override IRef<IDeclaration> ToIRef() => this.BoxedRef;

    IRef<INamespace> INamespace.ToRef() => this.BoxedRef;
}