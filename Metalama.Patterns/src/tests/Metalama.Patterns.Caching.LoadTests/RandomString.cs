// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Caching.LoadTests;

// These strings would be less random when using multi-threaded
internal static class RandomString
{
    // ReSharper disable StringLiteralTypo
    private static readonly char[] _allowedChars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#&@{}:;".ToCharArray();

    // Random is not thread-safe
    [ThreadStatic]
    private static Random? _random;

    // ReSharper restore StringLiteralTypo

    public static string New( int minLength, int maxLength )
    {
        _random ??= new Random();

        var length = _random.Next( minLength, maxLength + 1 );

        return New( length );
    }

    private static string New( int length )
    {
        _random ??= new Random();

        var chars = new char[length];

        for ( var i = 0; i < length; i++ )
        {
            chars[i] = _allowedChars[_random.Next( 0, _allowedChars.Length )];
        }

        return new string( chars );
    }
}