// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers
{
    internal sealed class GuidSerializer : ValueTypeSerializer<Guid>
    {
        public override void SerializeObject( Guid value, IArgumentsWriter writer )
        {
            writer.SetValue( "g", value.ToByteArray() );
        }

        public override Guid DeserializeObject( IArgumentsReader reader )
        {
            return new Guid( reader.GetValue<byte[]>( "g" )! );
        }
    }
}