// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace PostSharp.Serialization
{
    /// <summary>
    /// In Metalama, use <see cref="Metalama.Framework.Serialization.ReferenceTypeSerializer"/>.
    /// </summary>
    [PublicAPI]
    public abstract class ReferenceTypeSerializer : ISerializer
    {
        bool ISerializer.IsTwoPhase { get; }

        void ISerializer.DeserializeFields( ref object obj, IArgumentsReader initializationArguments )
        {
            throw new NotImplementedException();
        }

        void ISerializer.SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
        {
            throw new NotImplementedException();
        }

        public abstract object CreateInstance( Type type, IArgumentsReader constructorArguments );

        public abstract void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments );

        public abstract void DeserializeFields( object obj, IArgumentsReader initializationArguments );

        public virtual object Convert( object value, Type targetType )
        {
            throw new NotImplementedException();
        }
    }
}