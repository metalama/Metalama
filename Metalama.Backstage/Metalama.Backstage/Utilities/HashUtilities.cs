// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Text;

namespace Metalama.Backstage.Utilities
{
    internal static class HashUtilities
    {
        public static string HashToString( byte[] buffer ) => HexHelper.FormatBytes( new MD5Managed().ComputeHash( buffer ) );

        public static string HashToString( string s ) => HashToString( Encoding.UTF8.GetBytes( s ) );

        /// <summary>
        /// Computes an invariant 64-bit hash of a string.
        /// </summary>
        /// <param name="s">A string.</param>
        /// <returns>An invariant 64-bit hash of <paramref name="s"/>.</returns>
        public static long HashToInt64( string? s )
        {
            if ( s == null )
            {
                return 0;
            }

            s = s.Trim().ToLowerInvariant().Normalize();
            var bytes = Encoding.UTF8.GetBytes( s );

            byte[] hash;

            using ( var md5 = new MD5Managed() )
            {
                hash = md5.ComputeHash( bytes );
            }

            long hash64;

            unsafe
            {
                fixed ( byte* p = hash )
                {
                    hash64 = *(long*) p;
                }
            }

            // Make sure we never return 0 for a non-null string.
            if ( hash64 == 0 )
            {
                hash64 = -1;
            }

            return hash64;
        }
    }
}