// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Implementation;

[PublicAPI]
public ref struct StringTokenizer
{
    private readonly ReadOnlySpan<char> _s;
    private int _position;

    public StringTokenizer( string s ) : this( s.AsSpan() ) { }

    public StringTokenizer( ReadOnlySpan<char> s )
    {
        this._s = s;
        this._position = 0;
    }

    public ReadOnlySpan<char> GetNext( char separator )
    {
        var oldPosition = this._position;

        for ( var i = oldPosition; i < this._s.Length; i++ )
        {
            if ( this._s[i] == separator )
            {
                this._position = i + 1;

                return this._s.Slice( oldPosition, i - oldPosition );
            }
        }

        return this.GetRemainder();
    }

    public ReadOnlySpan<char> GetRemainder()
    {
        var oldPosition = this._position;
        this._position = this._s.Length;

        return this._s.Slice( oldPosition );
    }
}