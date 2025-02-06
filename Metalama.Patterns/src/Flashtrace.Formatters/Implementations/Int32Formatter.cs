// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.Implementations;

/// <summary>
/// Efficient formatter for <see cref="int"/>.
/// </summary>
internal sealed class Int32Formatter : Formatter<int>
{
    public Int32Formatter( IFormatterRepository repository ) : base( repository ) { }

    /// <inheritdoc />
    public override void Format( UnsafeStringBuilder stringBuilder, int value )
    {
        stringBuilder.Append( value );
    }
}