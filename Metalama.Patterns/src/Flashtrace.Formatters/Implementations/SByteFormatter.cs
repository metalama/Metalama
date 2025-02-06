// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.Implementations;

/// <summary>
/// Efficient formatter for <see cref="sbyte"/>.
/// </summary>
internal sealed class SByteFormatter : Formatter<sbyte>
{
    public SByteFormatter( IFormatterRepository repository ) : base( repository ) { }

    /// <inheritdoc />
    public override void Format( UnsafeStringBuilder stringBuilder, sbyte value )
    {
        stringBuilder.Append( value );
    }
}