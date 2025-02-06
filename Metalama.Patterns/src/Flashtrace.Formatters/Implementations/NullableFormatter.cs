// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.Implementations;

/// <summary>
/// Formatter for <see cref="Nullable{T}"/>.
/// </summary>
internal sealed class NullableFormatter<T> : Formatter<T?>
    where T : struct
{
    public NullableFormatter( IFormatterRepository repository ) : base( repository ) { }

    /// <inheritdoc />
    public override void Format( UnsafeStringBuilder stringBuilder, T? value )
    {
        if ( value == null )
        {
            stringBuilder.Append( 'n' );
            stringBuilder.Append( 'u' );
            stringBuilder.Append( 'l' );
            stringBuilder.Append( 'l' );
        }
        else
        {
            this.Repository.Get<T>().Format( stringBuilder, value.Value );
        }
    }
}