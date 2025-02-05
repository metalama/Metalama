// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
        var keys = dictionary.Keys.ToArray();
        var values = dictionary.Values.ToArray();

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