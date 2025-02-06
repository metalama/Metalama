// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Activities;
using System.Globalization;

namespace Flashtrace.Messages;

/// <summary>
/// Parses the formatting string for messages of the <see cref="FlashtraceLevelSource"/> and <see cref="LogActivity{TActivityDescription}"/> classes.
/// </summary>
internal ref struct FormattingStringParser
{
    private readonly ReadOnlySpan<char> _str;
    private int _position;
    private bool _hasParameter;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormattingStringParser"/> struct.
    /// Initializes a new <see cref="FormattingStringParser"/>.
    /// </summary>
    /// <param name="formattingString">The formatting string.</param>
    public FormattingStringParser( ReadOnlySpan<char> formattingString )
    {
        this._str = formattingString;
        this._position = 0;
        this._hasParameter = false;
    }

    private int GetIndexOf( char separator, out bool unescape )
    {
        unescape = false;

        if ( this._position >= this._str.Length )
        {
            return -1;
        }

        var cursor = this._position;

        while ( true )
        {
            var index = this._str.Slice( cursor ).IndexOf( separator );

            if ( index < 0 )
            {
                return -1;
            }

            index += cursor;

            if ( index < this._str.Length - 1 && this._str[index + 1] == separator )
            {
                unescape = true;
                cursor = index + 2;

                if ( cursor >= this._str.Length )
                {
                    return -1;
                }

                continue;
            }

            return index;
        }
    }

    /// <summary>
    /// Gets the next substring (until the next parameter or the end of the string).
    /// </summary>
    /// <returns>The next substring.</returns>
    public ReadOnlySpan<char> GetNextText()
    {
        var index = this.GetIndexOf( '{', out var unescape );
        var oldPosition = this._position;

        if ( index < 0 )
        {
            this._hasParameter = false;
            this._position = this._str.Length;

            return this.GetUnescapedString( oldPosition, this._str.Length - oldPosition, unescape );
        }
        else
        {
            this._hasParameter = true;
            this._position = index + 1;

            return this.GetUnescapedString( oldPosition, index - oldPosition, unescape );
        }
    }

    private ReadOnlySpan<char> GetUnescapedString( int index, int count, bool unescape )
    {
        unescape = unescape || this._str.Slice( index, count ).IndexOf( "}}".AsSpan() ) >= 0;

        if ( unescape )
        {
            var unescaped = new char[count];
            var unescapedCharacterCount = 0;

            for ( var i = 0; i < count; i++ )
            {
                unescaped[unescapedCharacterCount] = this._str[index + i];
                unescapedCharacterCount++;

                if ( i < count - 1 && (this._str[index + i] == '{' || this._str[index + i] == '}')
                                   && this._str[index + i] == this._str[index + i + 1] )
                {
                    i++;
                }
            }

            return new ReadOnlySpan<char>( unescaped, 0, unescapedCharacterCount );
        }
        else
        {
            return this._str.Slice( index, count );
        }
    }

    /// <summary>
    /// Gets the next parameter name.
    /// </summary>
    /// <returns>The name of the next parameter, or the default (null) value if there is no next parameter.</returns>
    public bool TryGetNextParameter( out ReadOnlySpan<char> span )
    {
        if ( !this._hasParameter )
        {
            span = default;

            return false;
        }

        var index = this.GetIndexOf( '}', out var unescape );

        if ( unescape )
        {
            throw new InvalidFormattingStringException( "Parameter names cannot contain escaped brackets." );
        }

        var oldPosition = this._position;

        if ( index < 0 )
        {
            throw new InvalidFormattingStringException(
                string.Format( CultureInfo.InvariantCulture, "Expected character '}}' after string index {0}.", this._position ) );
        }

        this._position = index + 1;

        span = this._str.Slice( oldPosition, index - oldPosition );

        return true;
    }
}