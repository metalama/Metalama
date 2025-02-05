// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine;
using System.Collections;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace System.Linq;

public static partial class LinqExtensions
{
    private interface INonMaterialized;

    private class ConcatenatedList<T> : INonMaterialized, IReadOnlyList<T>
    {
        private readonly IReadOnlyList<T> _list1;
        private readonly IReadOnlyList<T> _list2;
        private readonly IReadOnlyList<T> _list3;

#if DEBUG
        private bool _isAlreadyEvaluated;
#endif

        public ConcatenatedList( IReadOnlyList<T> list1, IReadOnlyList<T> list2, IReadOnlyList<T>? list3 = null )
        {
            this._list1 = list1;
            this._list2 = list2;
            this._list3 = list3 ?? Array.Empty<T>();
        }

        public IEnumerator<T> GetEnumerator()
        {
#if DEBUG

            // This is a heuristic to check that we don't evaluate the func several times
            // for the same item. In this case, the current class should not be used
            // and the query should be materialized.

            if ( this._isAlreadyEvaluated )
            {
                throw new AssertionFailedException( "The ConcatenatedList was evaluated twice." );
            }
            else
            {
                this._isAlreadyEvaluated = true;
            }

#endif

            if ( this._list1.Count > 0 )
            {
                foreach ( var item in this._list1 )
                {
                    yield return item;
                }
            }

            if ( this._list2.Count > 0 )
            {
                foreach ( var item in this._list2 )
                {
                    yield return item;
                }
            }

            if ( this._list3.Count > 0 )
            {
                foreach ( var item in this._list3 )
                {
                    yield return item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int Count => this._list1.Count + this._list2.Count + this._list3.Count;

        public T this[ int index ]
        {
            get
            {
                if ( index < this._list1.Count )
                {
                    return this._list1[index];
                }
                else if ( index < this._list1.Count + this._list2.Count )
                {
                    return this._list2[index - this._list1.Count];
                }
                else
                {
                    return this._list3[index - this._list1.Count - this._list3.Count];
                }
            }
        }
    }
}