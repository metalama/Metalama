// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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