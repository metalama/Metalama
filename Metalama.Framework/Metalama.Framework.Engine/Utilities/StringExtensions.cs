// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Metalama.Framework.Engine.Utilities
{
    [PublicAPI]
    public static class StringExtensions
    {
        internal static string TrimSuffix( this string s, string suffix )
            => s.EndsWith( suffix, StringComparison.Ordinal ) ? s.Substring( 0, s.Length - suffix.Length ) : s;

        public static string ReplaceOrdinal( this string s, string oldValue, string newValue )
#if NET5_0_OR_GREATER
            => s.Replace( oldValue, newValue, StringComparison.Ordinal );
#else
            => s.Replace( oldValue, newValue );
#endif

        public static bool ContainsOrdinal( this string s, string substring )
#if NET5_0_OR_GREATER
            => s.Contains( substring, StringComparison.Ordinal );
#else
            => s.Contains( substring );
#endif

        public static bool ContainsOrdinal( this string s, char c )
#if NET5_0_OR_GREATER
            => s.Contains( c, StringComparison.Ordinal );
#else
            => s.IndexOf( c ) >= 0;
#endif

        public static int IndexOfOrdinal( this string s, char c )
#if NET5_0_OR_GREATER
            => s.IndexOf( c, StringComparison.Ordinal );
#else
            => s.IndexOf( c );
#endif

        public static string NotNull( this string? s ) => s.AssertNotNull( s );

        public static void AppendLineInvariant( this StringBuilder stringBuilder, FormattableString s )
            => stringBuilder.AppendLine( FormattableString.Invariant( s ) );

        public static void AppendInvariant( this StringBuilder stringBuilder, FormattableString s ) => stringBuilder.Append( FormattableString.Invariant( s ) );

        public static int GetHashCodeOrdinal( this string s )
#if NET5_0_OR_GREATER
            => s.GetHashCode( StringComparison.Ordinal );
#else
            => s.GetHashCode();
#endif

        public static string ToCamelCase( this string s )
        {
            var firstLetter = s[..1];

            return firstLetter.ToLowerInvariant() + (s.Length > 1 ? s[1..] : "");
        }

        private static readonly Regex _invalidIdentifierCharacters = new( "[^_0-9a-zA-Z]", RegexOptions.Compiled );

        public static string ToIdentifier( this string s )
        {
            s = _invalidIdentifierCharacters.Replace( s, string.Empty );

            if ( s == string.Empty || !(char.IsLetter( s[0] ) || s[0] == '_') )
            {
                s = '_' + s;
            }

            return s;
        }

        internal static bool AnySegmentEquals( this string input, char[] separators, string item )
        {
            var index = 0;

            while ( index < input.Length )
            {
                var nextIndex = input.IndexOfAny( separators, index );
                
                if ( nextIndex == -1 )
                {
                    nextIndex = input.Length;
                }

                if ( input.AsSpan()[index..nextIndex].SequenceEqual( item.AsSpan() ) )
                {
                    return true;
                }

                index = nextIndex + 1;
            }
            
            return false;
        }
    }
}