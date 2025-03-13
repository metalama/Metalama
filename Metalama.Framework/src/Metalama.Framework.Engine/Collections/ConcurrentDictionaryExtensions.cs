// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// ReSharper disable once CheckNamespace

namespace System.Collections.Concurrent;

public static class ConcurrentDictionaryExtensions
{
    public static TValue GetOrAddNew<TKey, TValue>( this ConcurrentDictionary<TKey, TValue> dictionary, TKey key )
        where TValue : new()
        where TKey : notnull
        => dictionary.GetOrAdd( key, _ => new TValue() );

#if !NET6_0_OR_GREATER
    public static TValue GetOrAdd<TKey, TValue, TArg>(
        this ConcurrentDictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, TArg, TValue> valueFactory,
        TArg factoryArgument )
    {
        if ( dictionary.TryGetValue( key, out var value ) )
        {
            return value;
        }
        else
        {
            value = valueFactory( key, factoryArgument );

            return dictionary.GetOrAdd( key, value );
        }
    }

#endif
}