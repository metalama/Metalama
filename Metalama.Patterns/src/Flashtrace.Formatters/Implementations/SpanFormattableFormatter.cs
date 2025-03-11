// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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