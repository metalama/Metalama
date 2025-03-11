// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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