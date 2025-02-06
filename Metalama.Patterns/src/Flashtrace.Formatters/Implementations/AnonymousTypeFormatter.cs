// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters.Utilities;

namespace Flashtrace.Formatters.Implementations;

/// <summary>
/// The formatter used to for anonymous types by default.
/// </summary>
internal sealed class AnonymousTypeFormatter : IFormatter
{
    private readonly Func<object?, UnknownObjectAccessor> _accessorFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnonymousTypeFormatter"/> class for the specified <see cref="Type"/>.
    /// </summary>
    public AnonymousTypeFormatter( IFormatterRepository repository, Type type )
    {
        if ( type == null )
        {
            throw new ArgumentNullException( nameof(type) );
        }

        this.Repository = repository ?? throw new ArgumentNullException( nameof(repository) );
        this._accessorFactory = UnknownObjectAccessor.GetFactory( type );
    }

    public IFormatterRepository Repository { get; }

    /// <inheritdoc/>
    public FormatterAttributes Attributes => FormatterAttributes.Normal;

    /// <inheritdoc/>
    public void Format( UnsafeStringBuilder stringBuilder, object? value )
    {
        var accessor = this._accessorFactory( value );

        stringBuilder.Append( '{', ' ' );

        var i = 0;

        foreach ( var property in accessor )
        {
            if ( i > 0 )
            {
                stringBuilder.Append( ',', ' ' );
            }

            if ( property.Value != null )
            {
                stringBuilder.Append( property.Key );
                stringBuilder.Append( ' ', '=', ' ' );

                this.Repository.Get( property.Value.GetType() ).Format( stringBuilder, property.Value );
            }

            i++;
        }

        stringBuilder.Append( ' ', '}' );
    }
}