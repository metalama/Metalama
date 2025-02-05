// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Engine.CodeModel.UpdatableCollections;

#pragma warning disable SA1402

internal abstract class DeclarationUpdatableCollection<TDeclaration> : DeclarationUpdatableCollection<TDeclaration, IFullRef<TDeclaration>>
    where TDeclaration : class, IDeclaration
{
    protected DeclarationUpdatableCollection( CompilationModel compilation ) : base( compilation ) { }

    public override ImmutableArray<IFullRef<TDeclaration>> OfName( string name ) => this.Where( r => r.Name == name ).ToImmutableArray();
}

internal abstract class DeclarationUpdatableCollection<TDeclaration, TRef> : BaseDeclarationCollection, ILazy, IUpdatableCollection<TRef>
    where TDeclaration : class, IDeclaration
    where TRef : class, IRef<TDeclaration>
{
    private List<TRef>? _allItems;
    private volatile int _removeOperationsCount;

    protected DeclarationUpdatableCollection( CompilationModel compilation ) : base( compilation ) { }

    /// <summary>
    /// Gets a value indicating whether the construction has been populated for all names.
    /// </summary>
    public bool IsComplete { get; private set; }

    protected void EnsureComplete()
    {
        if ( this.IsComplete )
        {
            return;
        }

        lock ( this )
        {
            if ( this.IsComplete )
            {
                return;
            }

            using ( StackOverflowHelper.Detect() )
            {
                this._allItems = new List<TRef>();
                this.PopulateAllItems( r => this._allItems.Add( r ) );
                this.IsComplete = true;
            }
        }
    }

    protected abstract void PopulateAllItems( Action<TRef> action );

    protected void AddItem( in TRef item )
    {
        if ( this.IsComplete )
        {
            this._allItems!.Add( item );
        }
    }

    protected void InsertItem( int index, in TRef item )
    {
        if ( this.IsComplete )
        {
            this._allItems!.Insert( index, item );
        }
    }

    protected void RemoveItem( in TRef item )
    {
        if ( this.IsComplete )
        {
            var itemCopy = item;
            Interlocked.Increment( ref this._removeOperationsCount );
            this._allItems!.RemoveAll( i => i.Equals( itemCopy ) );
        }
    }

    public int Count
    {
        get
        {
            this.EnsureComplete();

            return this._allItems!.Count;
        }
    }

    public bool Contains( IRef<TDeclaration> item )
    {
        this.EnsureComplete();

        return this._allItems!.Any( i => i.Equals( item ) );
    }

    public IUpdatableCollection Clone( CompilationModel compilation )
    {
        var clone = (DeclarationUpdatableCollection<TDeclaration, TRef>) this.MemberwiseClone();
        clone.Compilation = compilation;

        if ( this._allItems != null )
        {
            lock ( this )
            {
                clone._allItems = new List<TRef>( this._allItems.Count );
                clone._allItems.AddRange( this._allItems );
            }
        }

        return clone;
    }

    public abstract ImmutableArray<TRef> OfName( string name );

    IEnumerator<TRef> IEnumerable<TRef>.GetEnumerator() => this.GetEnumerator();

    public Enumerator GetEnumerator() => new( this );

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public TRef this[ int index ]
    {
        get
        {
            this.EnsureComplete();

            return this._allItems![index];
        }
    }

    public struct Enumerator : IEnumerator<TRef>
    {
        private readonly DeclarationUpdatableCollection<TDeclaration, TRef> _parent;
        private readonly int _initialCount;
        private readonly int _initialRemoveOperationsCount;
        private int _index = -1;

        internal Enumerator( DeclarationUpdatableCollection<TDeclaration, TRef> parent )
        {
            this._parent = parent;

            // In case elements are added while iterating, we only return the items that were present when iteration started.
            this._initialCount = parent.Count;

            // In case elements are removed while iterating, we fail.
            this._initialRemoveOperationsCount = parent._removeOperationsCount;
        }

        public bool MoveNext()
        {
            if ( this._index + 1 < this._initialCount )
            {
                if ( this._parent._removeOperationsCount != this._initialRemoveOperationsCount )
                {
                    throw new InvalidOperationException( "An item was removed from the collection while an enumeration was in progress." );
                }

                this._index++;

                return true;
            }
            else
            {
                return false;
            }
        }

        public void Reset() => this._index = -1;

        public readonly TRef Current => this._parent[this._index];

        readonly object IEnumerator.Current => this.Current;

        public readonly void Dispose() { }
    }
}