// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Collections
{
    [PublicAPI]
    public interface IReadOnlyMultiValueDictionary<TKey, out TValue> : IEnumerable<IGrouping<TKey, TValue>>
        where TKey : notnull
    {
        IReadOnlyCollection<TValue> this[ TKey key ] { get; }

        IEnumerable<TKey> Keys { get; }
    }
}