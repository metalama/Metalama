// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities.Roslyn;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.UpdatableCollections;

internal sealed class AttributeUpdatableCollection : DeclarationUpdatableCollection<IAttribute, AttributeRef>
{
    private readonly IRef<IDeclaration> _parent;

    public AttributeUpdatableCollection( CompilationModel compilation, IRef<IDeclaration> parent ) : base( compilation )
    {
        this._parent = parent;

#if DEBUG
        (this._parent as ISymbolRef)?.Symbol.ThrowIfBelongsToDifferentCompilationThan( compilation.CompilationContext );
#endif
    }

    protected override void PopulateAllItems( Action<AttributeRef> action )
    {
        this._parent.AsFullRef().EnumerateAttributes( this.Compilation, action );
    }

    public override ImmutableArray<AttributeRef> OfName( string name ) => this.Where( r => r.Name == name ).ToImmutableArray();

    public void Add( AttributeBuilderData attribute )
    {
        this.EnsureComplete();
        this.AddItem( attribute.ToRef() );
    }

    public void Remove( IFullRef<INamedType> namedType )
    {
        this.EnsureComplete();

        var namedTypeDecl = namedType.ConstructedDeclaration;
        var itemsToRemove = this.Where( x => x.AttributeType.ToFullRef( namedType.RefFactory ).IsConvertibleTo( namedTypeDecl ) ).ToMutableList();

        foreach ( var item in itemsToRemove )
        {
            this.RemoveItem( item );
        }
    }
}