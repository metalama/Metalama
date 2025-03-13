// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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