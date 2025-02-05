// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Collections;

internal sealed class ReadOnlyCollectionWrapper<T> : IReadOnlyCollection<T>
{
    private readonly ICollection<T> _collection;

    public ReadOnlyCollectionWrapper( ICollection<T> collection )
    {
        this._collection = collection;
    }

    public IEnumerator<T> GetEnumerator() => this._collection.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public int Count => this._collection.Count;
}