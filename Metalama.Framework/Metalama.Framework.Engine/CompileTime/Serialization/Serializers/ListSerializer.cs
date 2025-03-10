// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Serialization;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers;

internal sealed class ListSerializer<T> : ReferenceTypeSerializer
{
    private const string _keyName = "_";

    /// <exclude/>
    public override object CreateInstance( Type type, IArgumentsReader constructorArguments )
    {
        // Assertion on nullability was added after the code import from PostSharp.
        var values = constructorArguments.GetValue<T[]>( _keyName ).AssertNotNull();
        var list = new List<T>( values.Length );
        list.AddRange( values );

        return list;
    }

    /// <exclude/>
    public override void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
    {
        var list = (List<T>) obj;

        // we need to save arrays in constructorArguments because objects from initializationArguments can be not fully deserialized when DeserializeFields is called
        constructorArguments.SetValue( _keyName, list.ToArray() );
    }

    /// <exclude/>
    public override void DeserializeFields( object obj, IArgumentsReader initializationArguments ) { }
}