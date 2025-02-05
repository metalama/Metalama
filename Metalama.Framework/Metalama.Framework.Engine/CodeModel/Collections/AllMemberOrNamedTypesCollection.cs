// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Services;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Metalama.Framework.Engine.CodeModel.Collections;

internal abstract class AllMemberOrNamedTypesCollection<TItem, TCollection> : IMemberOrNamedTypeCollection<TItem>
    where TItem : class, IMemberOrNamedType
    where TCollection : class, IMemberOrNamedTypeCollection<TItem>
{
    private volatile HashSet<TItem>? _members;

    protected AllMemberOrNamedTypesCollection( INamedType declaringType )
    {
        this.DeclaringType = declaringType;
    }

    protected CompilationContext CompilationContext => this.DeclaringType.GetCompilationContext();

    protected INamedType DeclaringType { get; }

    public IEnumerable<TItem> OfName( string name ) => this.GetItemsCore( name );

    public IEnumerator<TItem> GetEnumerator() => this.GetItems().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    protected abstract TCollection GetMembers( INamedType namespaceOrNamedType );

    public int Count => this.GetItems().Count;

    protected abstract IEqualityComparer<TItem> Comparer { get; }

    private HashSet<TItem> GetItemsCore( string? name )
    {
        // We don't assign the field directly so we don't get into concurrent updates of the collection.
        var members = new HashSet<TItem>( this.Comparer );

        for ( var t = this.DeclaringType; t != null; t = t.BaseType )
        {
            var includePrivate = ReferenceEquals( t, this.DeclaringType );
            var declaredMembers = name == null ? this.GetMembers( t ) : this.GetMembers( t ).OfName( name );

            foreach ( var member in declaredMembers )
            {
                if ( includePrivate || member.Accessibility != Accessibility.Private )
                {
                    members.Add( member );
                }
            }
        }

        return members;
    }

    private HashSet<TItem> GetItems()
    {
        if ( this._members == null )
        {
            Interlocked.CompareExchange( ref this._members, this.GetItemsCore( null ), null );
        }

        return this._members;
    }
}