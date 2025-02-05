// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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