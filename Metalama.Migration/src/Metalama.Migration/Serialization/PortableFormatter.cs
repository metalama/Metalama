// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using PostSharp.Reflection;
using System;
using System.IO;

namespace PostSharp.Serialization
{
    /// <summary>
    /// No equivalent in Metalama.
    /// </summary>
    [Obsolete( "", true )]
    [PublicAPI]
    public class PortableFormatter
    {
        public PortableFormatter() { }

        public static PortableSerializationBinder DefaultBinder { get; set; }

        public PortableFormatter( PortableSerializationBinder binder, ISerializerFactoryProvider serializerProvider )
        {
            throw new NotImplementedException();
        }

        public void Serialize( object obj, Stream stream )
        {
            throw new NotImplementedException();
        }

        public object Deserialize( Stream stream )
        {
            throw new NotImplementedException();
        }

        public IMetadataDispenser MetadataDispenser { get; set; }

        public IMetadataEmitter MetadataEmitter { get; set; }
    }
}