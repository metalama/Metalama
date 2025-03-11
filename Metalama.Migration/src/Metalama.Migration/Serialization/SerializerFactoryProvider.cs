// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace PostSharp.Serialization
{
    /// <summary>
    /// No equivalent in Metalama.
    /// </summary>
    [Obsolete( "", true )]
    [PublicAPI]
    public class SerializerFactoryProvider : ISerializerFactoryProvider
    {
        public static readonly SerializerFactoryProvider BuiltIn;

        public void MakeReadOnly()
        {
            throw new NotImplementedException();
        }

        public ISerializerFactoryProvider NextProvider { get; }

        // ReSharper disable UnusedTypeParameter
        public void AddSerializer<TObject, TSerializer>() where TSerializer : ISerializer, new()
        {
            throw new NotImplementedException();
        }

        public void AddSerializer( Type objectType, Type serializerType )
        {
            throw new NotImplementedException();
        }

        public virtual Type GetSurrogateType( Type objectType )
        {
            throw new NotImplementedException();
        }

        public virtual ISerializerFactory GetSerializerFactory( Type objectType )
        {
            throw new NotImplementedException();
        }
    }
}