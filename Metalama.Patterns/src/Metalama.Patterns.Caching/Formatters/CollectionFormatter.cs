// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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