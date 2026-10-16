// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers;

internal sealed class ImmutableDictionarySerializer<TKey, TValue> : ReferenceTypeSerializer
    where TKey : notnull
{
    // This needs to be a public type because the type is instantiated from an activator in client assemblies.
    private const string _comparerCodeName = "c";
    private const string _comparerName = "d";
    private const string _keysName = "k";
    private const string _valuesName = "v";

    /// <summary>
    /// The comparer that defines the order in which the keys are serialized, or <c>null</c> if the keys cannot be ordered.
    /// </summary>
    /// <remarks>
    /// An <see cref="ImmutableDictionary{TKey,TValue}"/> enumerates its keys in the order of their hash codes, and the hash code
    /// of a string is randomized per process. The keys are therefore sorted, so that the same dictionary produces the same bytes
    /// in any process (#2248).
    /// </remarks>
    private static readonly IComparer<TKey>? _keyComparer =
        typeof(TKey) == typeof(string) ? (IComparer<TKey>) StringComparer.Ordinal
        : typeof(IComparable<TKey>).IsAssignableFrom( typeof(TKey) ) ? Comparer<TKey>.Default
        : null;

    /// <exclude/>
    public override object CreateInstance( Type type, IArgumentsReader constructorArguments )
    {
        var comparerCode = constructorArguments.GetValue<byte>( _comparerCodeName );

        var comparer = constructorArguments.GetValue<IEqualityComparer<TKey>>( _comparerName ) ??
                       ComparerExtensions.GetComparerFromCode( comparerCode ) as IEqualityComparer<TKey>;

        var builder = ImmutableDictionary.CreateBuilder<TKey, TValue>( comparer );

        // Assertion on nullability was added after the code import from PostSharp.
        var keys = constructorArguments.GetValue<TKey[]>( _keysName ).AssertNotNull();
        var values = constructorArguments.GetValue<TValue[]>( _valuesName ).AssertNotNull();

        for ( var idx = 0; idx < keys.Length; idx++ )
        {
            builder[keys[idx]] = values[idx];
        }

        return builder.ToImmutable();
    }

    /// <exclude/>
    public override void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
    {
        var dictionary = (ImmutableDictionary<TKey, TValue>) obj;
        var pairs = dictionary.ToArray();

        var keyComparer = _keyComparer;

        if ( keyComparer != null )
        {
            Array.Sort( pairs, ( x, y ) => keyComparer.Compare( x.Key, y.Key ) );
        }

        var keys = new TKey[pairs.Length];
        var values = new TValue[pairs.Length];

        for ( var i = 0; i < pairs.Length; i++ )
        {
            keys[i] = pairs[i].Key;
            values[i] = pairs[i].Value;
        }

        var comparerCode = ComparerExtensions.GetComparerCode( dictionary.KeyComparer );

        // byte.MaxValue is a flag for custom comparer
        if ( comparerCode != byte.MaxValue )
        {
            constructorArguments.SetValue( _comparerCodeName, comparerCode );
        }
        else
        {
            constructorArguments.SetValue( _comparerName, dictionary.KeyComparer );
        }

        // we need to save arrays in constructorArguments because objects from initializationArguments can be not fully deserialized when DeserializeFields is called
        constructorArguments.SetValue( _keysName, keys );
        constructorArguments.SetValue( _valuesName, values );
    }

    /// <exclude/>
    public override void DeserializeFields( object obj, IArgumentsReader initializationArguments ) { }
}