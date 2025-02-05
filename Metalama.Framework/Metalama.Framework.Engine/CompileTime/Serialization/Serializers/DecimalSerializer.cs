// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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