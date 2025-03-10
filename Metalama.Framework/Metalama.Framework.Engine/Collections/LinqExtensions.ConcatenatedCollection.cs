// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace System.Linq;

public static partial class LinqExtensions
{
    private class ConcatenatedCollection<T> : INonMaterialized, IReadOnlyCollection<T>
    {
        private readonly IReadOnlyCollection<T> _collection1;
        private readonly IReadOnlyCollection<T> _collection2;

        public ConcatenatedCollection( IReadOnlyCollection<T> collection1, IReadOnlyCollection<T> collection2 )
        {
            this._collection1 = collection1;
            this._collection2 = collection2;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach ( var item in this._collection1 )
            {
                yield return item;
            }

            foreach ( var item in this._collection2 )
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int Count => this._collection1.Count + this._collection2.Count;
    }
}