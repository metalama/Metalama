// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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