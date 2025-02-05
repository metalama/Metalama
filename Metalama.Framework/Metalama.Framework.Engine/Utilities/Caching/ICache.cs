// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Engine.Utilities.Caching;

[PublicAPI]
internal interface ICache<TKey, TValue> : IDisposable
{
    bool TryGetValue( TKey key, out TValue value );

    TValue GetOrAdd( TKey key, Func<TKey, TValue> func );

    bool TryAdd( TKey key, TValue value );
}