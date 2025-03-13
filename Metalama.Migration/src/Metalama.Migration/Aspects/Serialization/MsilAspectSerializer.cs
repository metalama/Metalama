// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using PostSharp.Reflection;
using System;
using System.IO;

namespace PostSharp.Aspects.Serialization
{
    /// <summary>
    /// Aspect are also serializable in Metalama, but the serializer cannot be customized.
    /// </summary>
    public sealed class MsilAspectSerializer : AspectSerializer
    {
        public override void Serialize( IAspect[] aspects, Stream stream, IMetadataEmitter metadataEmitter )
        {
            throw new NotImplementedException();
        }

        protected override IAspect[] Deserialize( Stream stream, IMetadataDispenser metadataDispenser )
        {
            throw new NotImplementedException();
        }
    }
}