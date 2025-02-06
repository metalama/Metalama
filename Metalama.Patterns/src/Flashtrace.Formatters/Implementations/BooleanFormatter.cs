// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.Implementations;

/// <summary>
/// A formatter for <see cref="bool"/> values.
/// </summary>
internal sealed class BooleanFormatter : Formatter<bool>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BooleanFormatter"/> class.
    /// </summary>
    /// <param name="repository"></param>
    public BooleanFormatter( IFormatterRepository repository ) : base( repository ) { }

    /// <inheritdoc />
    public override void Format( UnsafeStringBuilder stringBuilder, bool value )
    {
        stringBuilder.Append( value );
    }
}