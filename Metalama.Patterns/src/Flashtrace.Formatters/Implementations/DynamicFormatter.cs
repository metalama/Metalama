// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Globalization;

namespace Flashtrace.Formatters.Implementations;

internal sealed class DynamicFormatter<TValue> : Formatter<TValue>
{
    private readonly FormattingOptions _options;

    public DynamicFormatter( IFormatterRepository repository ) : this( repository, FormattingOptions.Default ) { }

    private DynamicFormatter( IFormatterRepository repository, FormattingOptions? options )
        : base( repository )
    {
        this._options = options ?? FormattingOptions.Default;
    }

    private DynamicFormatter<TValue>? _otherFormatter;

    public override FormatterAttributes Attributes => FormatterAttributes.Dynamic;

    public override IOptionAwareFormatter WithOptions( FormattingOptions options )
    {
        if ( options == this._options )
        {
            return this;
        }
        else
        {
            // There are just two options currently.
            this._otherFormatter ??= new DynamicFormatter<TValue>( this.Repository, this._options );

            return this._otherFormatter;
        }
    }

    public override void Format( UnsafeStringBuilder stringBuilder, TValue? value )
    {
        if ( value == null )
        {
            stringBuilder.Append( 'n', 'u', 'l', 'l' );
        }
        else
        {
            var formatter = this.Repository.Get( value.GetType() ).WithOptions( this._options )
                            ?? throw new FormattersAssertionFailedException(
                                string.Format( CultureInfo.InvariantCulture, "Cannot get a formatter for type {0}.", value.GetType() ) );

            if ( (formatter.Attributes & FormatterAttributes.Dynamic) != 0 )
            {
                throw new FormattersAssertionFailedException(
                    string.Format( CultureInfo.InvariantCulture, "Infinite loop in resolving formatters for type {0}.", value.GetType() ) );
            }

            formatter.Format( stringBuilder, value );
        }
    }
}