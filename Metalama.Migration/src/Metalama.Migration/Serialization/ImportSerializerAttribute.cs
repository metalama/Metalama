// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace PostSharp.Serialization
{
    /// <summary>
    /// In Metalama, use <see cref="Metalama.Framework.Serialization.ImportSerializerAttribute"/>.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true )]
    [PublicAPI]
    public sealed class ImportSerializerAttribute : Attribute
    {
        public ImportSerializerAttribute( Type objectType, Type serializerType )
        {
            throw new NotImplementedException();
        }

        public Type ObjectType { get; }

        public Type SerializerType { get; }
    }
}