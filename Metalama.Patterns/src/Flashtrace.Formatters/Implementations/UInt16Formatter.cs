// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.Implementations;

/// <summary>
/// A formatter for <see cref="ushort"/> values.
/// </summary>
internal sealed class UInt16Formatter : Formatter<ushort>
{
    public UInt16Formatter( IFormatterRepository repository ) : base( repository ) { }

    /// <inheritdoc />
    public override void Format( UnsafeStringBuilder stringBuilder, ushort value )
    {
        stringBuilder.Append( value );
    }
}