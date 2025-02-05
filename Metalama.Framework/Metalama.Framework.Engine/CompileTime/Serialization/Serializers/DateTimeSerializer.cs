// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers
{
    internal sealed class DateTimeSerializer : ValueTypeSerializer<DateTime>
    {
        public override void SerializeObject( DateTime value, IArgumentsWriter writer )
        {
            writer.SetValue( "k", (byte) value.Kind );
            writer.SetValue( "t", value.Ticks );
        }

        public override DateTime DeserializeObject( IArgumentsReader reader )
        {
            return new DateTime( reader.GetValue<long>( "t" ), (DateTimeKind) reader.GetValue<byte>( "k" ) );
        }
    }
}