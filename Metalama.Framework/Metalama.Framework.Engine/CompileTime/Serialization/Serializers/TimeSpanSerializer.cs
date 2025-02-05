// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers
{
    internal sealed class TimeSpanSerializer : ValueTypeSerializer<TimeSpan>
    {
        public override void SerializeObject( TimeSpan value, IArgumentsWriter writer )
        {
            writer.SetValue( "t", value.Ticks );
        }

        public override TimeSpan DeserializeObject( IArgumentsReader reader )
        {
            return new TimeSpan( reader.GetValue<long>( "t" ) );
        }
    }
}