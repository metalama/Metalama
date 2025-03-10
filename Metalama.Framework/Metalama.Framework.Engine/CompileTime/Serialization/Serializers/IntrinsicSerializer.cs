// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers
{
    internal abstract class IntrinsicSerializer<T> : ISerializer
    {
        public abstract object Convert( object value, Type targetType );

        object ISerializer.CreateInstance( Type type, IArgumentsReader constructorArguments )
        {
            return constructorArguments.GetValue<T>( "_" )!;
        }

        void ISerializer.DeserializeFields( ref object obj, IArgumentsReader initializationArguments ) { }

        void ISerializer.SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter? initializationArguments )
        {
            constructorArguments.SetValue( "_", (T) obj );
        }

        public bool IsTwoPhase => false;
    }
}