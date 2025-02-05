// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if NETSTANDARD2_0 // ReSharper disable once CheckNamespace
namespace System.Collections.Generic;

public static class KeyValuePairExtensions
{
    public static void Deconstruct<TKey, TValue>( this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value )
    {
        key = pair.Key;
        value = pair.Value;
    }
}
#endif