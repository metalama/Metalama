// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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