// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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