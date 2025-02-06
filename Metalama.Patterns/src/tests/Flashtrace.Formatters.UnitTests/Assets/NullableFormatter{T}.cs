// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.UnitTests.Assets
{
    internal class NullableFormatter<T> : Formatter<T?>
        where T : struct
    {
        public NullableFormatter( IFormatterRepository repository ) : base( repository ) { }

        public override void Format( UnsafeStringBuilder stringBuilder, T? value )
        {
            stringBuilder.Append( '<' );
            stringBuilder.Append( value == null ? "null" : value.ToString() );
            stringBuilder.Append( '>' );
        }
    }
}