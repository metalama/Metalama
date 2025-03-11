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
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct )]
    [PublicAPI]
    public sealed class SerializerAttribute : Attribute
    {
        public SerializerAttribute( Type serializerType )
        {
            throw new NotImplementedException();
        }

        public Type SerializerType { get; }
    }
}