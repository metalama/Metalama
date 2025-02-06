// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.UnitTests.Assets
{
    internal class EnumerableFormatter<T> : Formatter<IEnumerable<T>>
    {
        public EnumerableFormatter( IFormatterRepository repository ) : base( repository ) { }

        public override void Format( UnsafeStringBuilder stringBuilder, IEnumerable<T>? value )
        {
            stringBuilder.Append( '[' );
            stringBuilder.Append( string.Join( ",", value! ) );
            stringBuilder.Append( ']' );
        }
    }
}