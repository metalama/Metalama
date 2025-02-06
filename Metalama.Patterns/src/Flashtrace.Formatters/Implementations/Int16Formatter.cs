// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.Implementations;

/// <summary>
/// Efficient formatter for <see cref="short"/>.
/// </summary>
internal sealed class Int16Formatter : Formatter<short>
{
    public Int16Formatter( IFormatterRepository repository ) : base( repository ) { }

    /// <inheritdoc />
    public override void Format( UnsafeStringBuilder stringBuilder, short value )
    {
        stringBuilder.Append( value );
    }
}