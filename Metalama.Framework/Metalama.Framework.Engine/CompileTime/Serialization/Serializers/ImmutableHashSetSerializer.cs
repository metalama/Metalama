// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers;

internal sealed class ImmutableHashSetSerializer<T> : ReferenceTypeSerializer
{
    // This needs to be a public type because the type is instantiated from an activator in client assemblies.
    private const string _comparerCodeName = "c";
    private const string _comparerName = "d";
    private const string _keysName = "k";

    /// <exclude/>
    public override object CreateInstance( Type type, IArgumentsReader constructorArguments )
    {
        var comparerCode = constructorArguments.GetValue<byte>( _comparerCodeName );

        var comparer = constructorArguments.GetValue<IEqualityComparer<T>>( _comparerName ) ??
                       ComparerExtensions.GetComparerFromCode( comparerCode ) as IEqualityComparer<T>;

        var builder = ImmutableHashSet.CreateBuilder( comparer );

        // Assertion on nullability was added after the code import from PostSharp.
        var keys = constructorArguments.GetValue<T[]>( _keysName ).AssertNotNull();

        foreach ( var t in keys )
        {
            builder.Add( t );
        }

        return builder.ToImmutable();
    }

    /// <exclude/>
    public override void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
    {
        var hashSet = (ImmutableHashSet<T>) obj;
        var keys = hashSet.ToArray();

        var comparerCode = ComparerExtensions.GetComparerCode( hashSet.KeyComparer );

        // byte.MaxValue is a flag for custom comparer
        if ( comparerCode != byte.MaxValue )
        {
            constructorArguments.SetValue( _comparerCodeName, comparerCode );
        }
        else
        {
            constructorArguments.SetValue( _comparerName, hashSet.KeyComparer );
        }

        // we need to save arrays in constructorArguments because objects from initializationArguments can be not fully deserialized when DeserializeFields is called
        constructorArguments.SetValue( _keysName, keys );
    }

    /// <exclude/>
    public override void DeserializeFields( object obj, IArgumentsReader initializationArguments ) { }
}