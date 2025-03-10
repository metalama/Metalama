// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Serialization;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers
{
    internal sealed class DecimalSerializer : ValueTypeSerializer<decimal>
    {
        public override void SerializeObject( decimal value, IArgumentsWriter writer )
        {
            writer.SetValue( "d", decimal.GetBits( value ) );
        }

        public override decimal DeserializeObject( IArgumentsReader reader )
        {
            return new decimal( reader.GetValue<int[]>( "d" )! );
        }
    }
}