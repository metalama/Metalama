// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.UnitTests.Assets
{
    internal class NonNullableFormatter<T> : Formatter<T>
        where T : struct
    {
        public NonNullableFormatter( IFormatterRepository repository ) : base( repository ) { }

        public override void Format( UnsafeStringBuilder stringBuilder, T value )
        {
            stringBuilder.Append( '[' );
            stringBuilder.Append( value.ToString() );
            stringBuilder.Append( ']' );
        }
    }
}