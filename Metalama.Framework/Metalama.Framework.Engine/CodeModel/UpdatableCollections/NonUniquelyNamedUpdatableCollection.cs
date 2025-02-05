// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CodeModel.UpdatableCollections;

internal abstract class NonUniquelyNamedUpdatableCollection<T> : MemberUpdatableCollection<T>
    where T : class, IMemberOrNamedType
{
    private ImmutableDictionary<string, MemberRefUpdatableArray<T>>? _byNameDictionary;

    protected NonUniquelyNamedUpdatableCollection( CompilationModel compilation, IRef declaringNamespaceOrType ) : base(
        compilation,
        declaringNamespaceOrType ) { }

    private ImmutableDictionary<string, MemberRefUpdatableArray<T>> GetInitializedByNameDictionary()
        => this._byNameDictionary ??= ImmutableDictionary<string, MemberRefUpdatableArray<T>>.Empty.WithComparers( StringComparer.Ordinal );

    public override ImmutableArray<IFullRef<T>> OfName( string name )
    {
        var byName = this.GetInitializedByNameDictionary();

        if ( byName.TryGetValue( name, out var updatableArray ) )
        {
            return updatableArray.Array;
        }
        else if ( !this.IsComplete )
        {
            var members = new MemberRefUpdatableArray<T>(
                this.GetMemberRefsOfName( name ).ToImmutableArray(),
                this.Compilation );

            this._byNameDictionary = byName.SetItem( name, members );

            return members.Array;
        }
        else
        {
            return ImmutableArray<IFullRef<T>>.Empty;
        }
    }

    protected override void PopulateAllItems( Action<IFullRef<T>> action )
    {
        var byNameDictionary = this.GetInitializedByNameDictionary();
        var byNameDictionaryBuilder = byNameDictionary.ToBuilder();

        // Add all items that already been discovered in a name-specific operation.
        foreach ( var array in byNameDictionary.Values )
        {
            foreach ( var item in array.Array )
            {
                action( item );
            }
        }

        // Discover from source.
        foreach ( var memberRef in this.GetMemberRefs() )
        {
            var name = memberRef.Name.AssertNotNull();

            // We intentionally look in the initial dictionary (not the builder). If there is no value for this name, it means
            // that the collection was not built for that name, and we need to create it now.

            if ( !byNameDictionary.ContainsKey( name ) )
            {
                action( memberRef );

                if ( !byNameDictionaryBuilder.TryGetValue( name, out var members ) )
                {
                    // This is the first time this method processes a member of that name.
                    members = new MemberRefUpdatableArray<T>( ImmutableArray.Create( memberRef ), this.Compilation );
                    byNameDictionaryBuilder[name] = members;
                }
                else
                {
                    // The current method has already processed a member of that name, so we need to update the existing collection.
                    members.Add( memberRef );
                }
            }
        }

        this._byNameDictionary = byNameDictionaryBuilder.ToImmutable();
    }

    public void Add( IFullRef<T> member )
    {
        var name = member.Name.AssertNotNull();

        var byNameDictionary = this.GetInitializedByNameDictionary();

        if ( !byNameDictionary.TryGetValue( name, out var members ) )
        {
            if ( !this.IsComplete )
            {
                // The collection has not been populated yet, so do it now, and add the new member.
                members = new MemberRefUpdatableArray<T>(
                    this.GetMemberRefsOfName( name ).ToImmutableArray().Add( member ),
                    this.Compilation );
            }
            else
            {
                // The collection has been populated and there is no item of that name, so only add the member.
                members = new MemberRefUpdatableArray<T>( ImmutableArray.Create( member ), this.Compilation );
            }

            this._byNameDictionary = byNameDictionary.SetItem( name, members );
        }
        else
        {
            // The collection has been populated. Add the new member.
            if ( ReferenceEquals( members.ParentCompilation, this.Compilation ) )
            {
                members.Add( member );
            }
            else
            {
                // The object was created for another compilation, so we need to create a clone for ourselves.
                members = new MemberRefUpdatableArray<T>( members.Array.Add( member ), this.Compilation );
                this._byNameDictionary = byNameDictionary.SetItem( name, members );
            }
        }

        this.AddItem( member );
    }

    protected abstract IEqualityComparer<IRef<T>> MemberRefComparer { get; }

    // TODO: Verify why Remove is never called.
    // Resharper disable UnusedMember.Global

    public void Remove( IFullRef<T> member )
    {
        var byNameDictionary = this.GetInitializedByNameDictionary();

        var name = member.Name.AssertNotNull();

        if ( !byNameDictionary.TryGetValue( name, out var members ) )
        {
            if ( !this.IsComplete )
            {
                // The collection has not been populated yet, so do it now, and add the new member.
                var sourceMembers = this.GetMemberRefsOfName( name ).ToImmutableArray();

                var index = sourceMembers.IndexOf( member, this.MemberRefComparer );

                if ( index < 0 )
                {
                    throw new AssertionFailedException( $"The collection does not contain the item '{member}'." );
                }

                members = new MemberRefUpdatableArray<T>( sourceMembers.RemoveAt( index ), this.Compilation );
            }
            else
            {
                throw new AssertionFailedException( $"The collection was populated, but it did not contain the item '{member}'." );
            }

            this._byNameDictionary = byNameDictionary.SetItem( name, members );
        }
        else
        {
            // The collection has been populated. Remove the member.
            if ( ReferenceEquals( members.ParentCompilation, this.Compilation ) )
            {
                members.Remove( member );
            }
            else
            {
                // The object was created for another compilation, so we need to create a clone for ourselves.
                var index = members.Array.IndexOf( member, this.MemberRefComparer );

                if ( index < 0 )
                {
                    throw new AssertionFailedException( $"The collection does not contain the item '{member}'." );
                }

                members = new MemberRefUpdatableArray<T>( members.Array.RemoveAt( index ), this.Compilation );
                this._byNameDictionary = byNameDictionary.SetItem( name, members );
            }
        }

        this.RemoveItem( member );
    }
}