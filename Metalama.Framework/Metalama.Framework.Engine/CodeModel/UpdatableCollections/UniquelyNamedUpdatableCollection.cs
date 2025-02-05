// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.CodeModel.References;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.UpdatableCollections;

internal abstract class UniquelyNamedUpdatableCollection<T> : MemberUpdatableCollection<T>
    where T : class, INamedDeclaration
{
    private ImmutableDictionary<string, IFullRef<T>?>? _dictionary;

    protected UniquelyNamedUpdatableCollection( CompilationModel compilation, IRef<INamespaceOrNamedType> containingDeclaration ) :
        base( compilation, containingDeclaration ) { }

    private ImmutableDictionary<string, IFullRef<T>?> GetInitializedDictionary()
        => this._dictionary ??= ImmutableDictionary<string, IFullRef<T>?>.Empty.WithComparers( StringComparer.Ordinal, RefEqualityComparer<T>.Default );

    public void Add( IFullRef<T> member )
    {
        var dictionary = this.GetInitializedDictionary();

        var name = member.Name.AssertNotNull();

        if ( dictionary.TryGetValue( name, out _ ) )
        {
            // The dictionary was populated, but there was no member with that name yet.
            this._dictionary = dictionary.SetItem( name, member );
        }
        else if ( this.IsComplete )
        {
            // The dictionary was populated, but there was no member with that name yet.
            this._dictionary = dictionary.SetItem( name, member );
        }
        else
        {
            // The dictionary was not yet populated.

            this._dictionary = dictionary.SetItem( name, member );
        }

        this.AddItem( member );
    }

    public void Remove( IFullRef<T> member )
    {
        var dictionary = this.GetInitializedDictionary();

        var name = member.Name.AssertNotNull();

        if ( dictionary.TryGetValue( name, out _ ) )
        {
            // The dictionary was populated, and there was was a member with that name, so remove it.
            this._dictionary = dictionary.SetItem( name, default );
        }
        else if ( this.IsComplete )
        {
            // Missing key.
            throw new AssertionFailedException( $"The collection does not contain '{member}'." );
        }
        else
        {
            // The dictionary was not yet populated.
            this._dictionary = dictionary.SetItem( name, default );
        }

        this.RemoveItem( member );
    }

    protected override void PopulateAllItems( Action<IFullRef<T>> action )
    {
        var dictionary = this.GetInitializedDictionary();

        // Add items that have already been retrieved.
        foreach ( var item in dictionary )
        {
            if ( item.Value != null )
            {
                action( item.Value );
            }
        }

        var dictionaryBuilder = dictionary.ToBuilder();

        // Add items discovered from source code.
        foreach ( var memberRef in this.GetMemberRefs() )
        {
            var name = memberRef.Name.AssertNotNull();

            if ( !dictionary.ContainsKey( name ) )
            {
                dictionaryBuilder[name] = memberRef;
                action( memberRef );
            }
            else
            {
                // This member has already been discovered by a previous call of this object.
            }
        }

        this._dictionary = dictionaryBuilder.ToImmutable();
    }

    public override ImmutableArray<IFullRef<T>> OfName( string name )
    {
        var dictionary = this.GetInitializedDictionary();

        if ( !dictionary.TryGetValue( name, out var member ) )
        {
            member = this.GetMemberRefsOfName( name ).FirstOrDefault();
            this._dictionary = dictionary.SetItem( name, member );
        }

        if ( member != null )
        {
            return ImmutableArray.Create( member );
        }
        else
        {
            return ImmutableArray<IFullRef<T>>.Empty;
        }
    }
}