// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Formatters;

namespace Metalama.Patterns.Caching.Formatters;

internal sealed class CollectionFormatter<T> : Formatter<IEnumerable<T>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionFormatter{T}"/> class using the specified <see cref="IFormatterRepository"/>
    /// to access formatters for other types.
    /// </summary>
    /// <param name="repository"></param>
    public CollectionFormatter( IFormatterRepository repository ) : base( repository ) { }

    /// <inheritdoc />
    public override void Format( UnsafeStringBuilder stringBuilder, IEnumerable<T>? value )
    {
        if ( value == null )
        {
            stringBuilder.Append( 'n', 'u', 'l', 'l' );

            return;
        }

        var formatter = this.Repository.Get<T>();

        var first = true;

        foreach ( var item in value )
        {
            if ( first )
            {
                stringBuilder.Append( '[', ' ' );
            }
            else
            {
                stringBuilder.Append( ',', ' ' );
            }

            first = false;

            formatter.Format( stringBuilder, item );
        }

        if ( first )
        {
            stringBuilder.Append( '[', ']' );
        }
        else
        {
            stringBuilder.Append( ' ', ']' );
        }
    }
}