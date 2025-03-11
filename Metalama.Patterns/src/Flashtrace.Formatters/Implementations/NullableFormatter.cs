// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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