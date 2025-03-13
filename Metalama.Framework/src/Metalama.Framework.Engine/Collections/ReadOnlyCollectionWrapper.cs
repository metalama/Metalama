// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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