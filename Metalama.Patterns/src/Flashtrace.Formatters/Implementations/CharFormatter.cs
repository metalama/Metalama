// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Flashtrace.Formatters.Implementations;

/// <summary>
/// A formatter for <see cref="char"/> values.
/// </summary>
internal sealed class CharFormatter : Formatter<char>
{
    private NonQuotingCharFormatter? _nonQuotingCharFormatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="CharFormatter"/> class.
    /// </summary>
    /// <param name="repository"></param>
    public CharFormatter( IFormatterRepository repository ) : base( repository ) { }

    private NonQuotingCharFormatter GetNonQuotingCharFormatter()
    {
        this._nonQuotingCharFormatter ??= new NonQuotingCharFormatter( this.Repository, this );

        return this._nonQuotingCharFormatter;
    }

    /// <inheritdoc />
    public override void Format( UnsafeStringBuilder stringBuilder, char value )
    {
        stringBuilder.Append( '\'', value, '\'' );
    }

    /// <inheritdoc />
    public override IOptionAwareFormatter WithOptions( FormattingOptions options )
    {
        if ( options.RequiresUnquotedStrings )
        {
            return this.GetNonQuotingCharFormatter();
        }
        else
        {
            return this;
        }
    }

    private sealed class NonQuotingCharFormatter : Formatter<char>
    {
        private readonly CharFormatter _defaultCharFormatter;

        public NonQuotingCharFormatter( IFormatterRepository repository, CharFormatter defaultCharFormatter ) : base( repository )
        {
            this._defaultCharFormatter = defaultCharFormatter;
        }

        public override void Format( UnsafeStringBuilder stringBuilder, char value )
        {
            if ( value == '\0' )
            {
                // Don't emit anything for \0. We would need another escaping formatter.
            }
            else
            {
                stringBuilder.Append( value );
            }
        }

        public override IOptionAwareFormatter WithOptions( FormattingOptions options )
        {
            if ( options.RequiresUnquotedStrings )
            {
                return this;
            }
            else
            {
                return this._defaultCharFormatter;
            }
        }
    }
}