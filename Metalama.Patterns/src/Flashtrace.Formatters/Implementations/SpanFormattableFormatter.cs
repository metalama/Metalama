// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if NET6_0_OR_GREATER
using Flashtrace.Formatters.TypeExtensions;

namespace Flashtrace.Formatters.Implementations;

internal sealed class SpanFormattableFormatter<[BindToExtendedType] TValue> : Formatter<TValue>
    where TValue : ISpanFormattable
{
    public SpanFormattableFormatter( IFormatterRepository repository ) : base( repository ) { }

    public override void Format( UnsafeStringBuilder stringBuilder, TValue? value )
    {
        if ( value == null )
        {
            stringBuilder.Append( 'n', 'u', 'l', 'l' );
        }
        else
        {
            stringBuilder.Append( value );
        }
    }
}

#endif