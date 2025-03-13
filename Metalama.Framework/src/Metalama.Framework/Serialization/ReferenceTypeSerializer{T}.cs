// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Serialization;

[PublicAPI]
public abstract class ReferenceTypeSerializer<T> : ReferenceTypeSerializer
    where T : class
{
    public override object CreateInstance( Type type, IArgumentsReader constructorArguments ) => this.CreateInstance( constructorArguments );

    public sealed override void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
        => this.SerializeObject( (T) obj, constructorArguments, initializationArguments );

    public sealed override void DeserializeFields( object obj, IArgumentsReader initializationArguments )
        => this.DeserializeFields( (T) obj, initializationArguments );

    public abstract T CreateInstance( IArgumentsReader constructorArguments );

    public abstract void SerializeObject( T obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments );

    public abstract void DeserializeFields( T obj, IArgumentsReader initializationArguments );
}