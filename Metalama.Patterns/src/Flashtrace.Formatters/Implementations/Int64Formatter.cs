// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.Implementations;

/// <summary>
/// Efficient formatter for <see cref="long"/>.
/// </summary>
internal sealed class Int64Formatter : Formatter<long>
{
    public Int64Formatter( IFormatterRepository repository ) : base( repository ) { }

    /// <inheritdoc />
    public override void Format( UnsafeStringBuilder stringBuilder, long value )
    {
        stringBuilder.Append( value );
    }
}