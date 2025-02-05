// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Source;

internal sealed class SourceNamespace : SourceDeclaration, INamespace
{
    private readonly INamespaceSymbol _symbol;

    internal SourceNamespace( INamespaceSymbol symbol, CompilationModel compilation ) : base( compilation, null )
    {
        this._symbol = symbol;
    }

    private bool IsExternal => this._symbol.ContainingAssembly != this.Compilation.RoslynCompilation.Assembly;

    public override IDeclaration? ContainingDeclaration
    {
        get
        {
            if ( this.IsGlobalNamespace )
            {
                if ( this.IsExternal )
                {
                    return this._symbol.ContainingAssembly == null ? null : this.DeclaringAssembly;
                }
                else
                {
                    return this.Compilation;
                }
            }
            else
            {
                return this.ContainingNamespace.AssertNotNull();
            }
        }
    }

    IRef<INamespaceOrNamedType> INamespaceOrNamedType.ToRef() => this.Ref;

    [Memo]
    public override IAssembly DeclaringAssembly
        => this.Compilation.Factory.GetAssembly(
            this._symbol.ContainingAssembly
            ?? throw new InvalidOperationException( "This namespace is a merged namespace for the whole compilation, so it has no declaring assembly." ) );

    public override DeclarationKind DeclarationKind => DeclarationKind.Namespace;

    public override ISymbol Symbol => this._symbol;

    public string Name => this._symbol.IsGlobalNamespace ? "" : this._symbol.Name;

    [Memo]
    public string FullName => this._symbol.GetFullName() ?? "";

    public bool IsGlobalNamespace => this._symbol.IsGlobalNamespace;

    public override bool CanBeInherited => false;

    public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default ) => [];

    [Memo]
    public INamespace? ContainingNamespace => this.IsGlobalNamespace ? null : this.Compilation.Factory.GetNamespace( this._symbol.ContainingNamespace );

    // TODO: TypeUpdatableCollection could be cached in the CompilationModel.
    public INamedTypeCollection Types
    {
        get
        {
            if ( !this.IsExternal )
            {
                OnUnsupportedDependency( $"{nameof(INamespace)}.{nameof(this.Types)}" );
            }

            return this.TypesCore;
        }
    }

    [Memo]
    private INamedTypeCollection TypesCore
        => new NamedTypeCollection(
            this.Compilation,
            this.Compilation.GetNamedTypeCollectionByParent( this._symbol.ToRef( this.RefFactory ) ) );

    // TODO: AllNamespaceTypesUpdateableCollection could be cached in the CompilationModel.

    public INamespaceCollection Namespaces
    {
        get
        {
            if ( !this.IsExternal )
            {
                OnUnsupportedDependency( $"{nameof(INamespace)}.{nameof(this.Namespaces)}" );
            }

            return this.NamespacesCore;
        }
    }

    [Memo]
    private INamespaceCollection NamespacesCore
        => new NamespaceCollection(
            this,
            this.Compilation.GetNamespaceCollection( this._symbol.ToRef( this.RefFactory ) ) );

    public INamespace? GetDescendant( string ns )
    {
        // Fast track: try with a symbol.
        var s = this._symbol.GetDescendant( ns );

        if ( s != null )
        {
            return this.Compilation.Factory.GetNamespace( s );
        }
        else
        {
            // Slow track: take builders into account.
            var namespaceCursor = this.Ref;

            foreach ( var part in ns.Split( '.' ) )
            {
                namespaceCursor = this.Compilation.GetNamespaceCollection( namespaceCursor ).OfName( part ).FirstOrDefault();

                if ( namespaceCursor == null )
                {
                    return null;
                }
            }

            return namespaceCursor.GetTarget( this.Compilation );
        }
    }

    public bool IsPartial => !this.IsExternal && this.Compilation.IsPartial;

    INamespace? INamespace.ParentNamespace => this.ContainingNamespace;

    public override string ToString() => $"{(this.IsGlobalNamespace ? "<Global Namespace>" : this.FullName)} ({this.DeclaringAssembly.Identity.Name})";

    public override string ToDisplayString( CodeDisplayFormat? format = null, CodeDisplayContext? context = null ) => this.FullName;

    public override SyntaxTree? PrimarySyntaxTree => null;

    [Memo]
    private IFullRef<INamespace> Ref => this.RefFactory.FromSymbolBasedDeclaration<INamespace>( this );

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    IRef<INamespace> INamespace.ToRef() => this.Ref;
}