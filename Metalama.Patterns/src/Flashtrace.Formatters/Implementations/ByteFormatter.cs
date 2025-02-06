// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.Implementations;

/// <summary>
/// A formatter for <see cref="byte"/> values.
/// </summary>
internal sealed class ByteFormatter : Formatter<byte>
{
    public ByteFormatter( IFormatterRepository repository ) : base( repository ) { }

    /// <inheritdoc />
    public override void Format( UnsafeStringBuilder stringBuilder, byte value )
    {
        stringBuilder.Append( value );
    }
}