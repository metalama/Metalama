// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.Implementations;

/// <summary>
/// A formatter for <see cref="ulong"/> values.
/// </summary>
internal sealed class UInt64Formatter : Formatter<ulong>
{
    public UInt64Formatter( IFormatterRepository repository ) : base( repository ) { }

    /// <inheritdoc />
    public override void Format( UnsafeStringBuilder stringBuilder, ulong value )
    {
        stringBuilder.Append( value );
    }
}