// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.Implementations;

/// <summary>
/// A formatter for <see cref="uint"/> values.
/// </summary>
internal sealed class UInt32Formatter : Formatter<uint>
{
    public UInt32Formatter( IFormatterRepository repository ) : base( repository ) { }

    /// <inheritdoc />
    public override void Format( UnsafeStringBuilder stringBuilder, uint value )
    {
        stringBuilder.Append( value );
    }
}