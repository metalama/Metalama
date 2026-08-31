// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text;

namespace Metalama.Backstage.Utilities
{
    internal static class HashUtilities
    {
        public static string HashToString( byte[] buffer ) => HexHelper.FormatBytes( XxHash128.Hash( buffer ) );

        public static string HashToString( string s ) => HashToString( Encoding.UTF8.GetBytes( s ) );

        /// <summary>
        /// Computes an invariant 64-bit hash of a string.
        /// </summary>
        /// <param name="s">A string.</param>
        /// <returns>An invariant 64-bit hash of <paramref name="s"/>.</returns>
        public static long ComputeInt64Hmac( string? s, long salt )
        {
            if ( s == null )
            {
                return 0;
            }

            s = s.Trim().ToLowerInvariant().Normalize();

            var bytes = Encoding.UTF8.GetBytes( s );
            var saltBytes = new byte[8];

            unsafe
            {
                fixed ( byte* p = saltBytes )
                {
                    *(long*) p = salt;
                }
            }

            byte[] hash;

            using ( var hmac = new HMACSHA256( saltBytes ) )
            {
                hash = hmac.ComputeHash( bytes );
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

        /// <summary>
        /// Computes an unkeyed 64-bit hash of a string, using the same algorithm as the
        /// <c>CryptoUtilities.ComputeStringHash64</c> method of PostSharp.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The value is not salted, so the same input gives the same value on every machine, in every version, and in
        /// both PostSharp and Metalama. The license audit uses this method so that one person is counted once,
        /// whatever the number of machines that person uses and whatever the mixture of the two products. See issue
        /// #1873.
        /// </para>
        /// <para>
        /// The algorithm is MD5. MD5 is not chosen for its cryptographic properties, which are irrelevant here, but
        /// because the values must be equal to the values that PostSharp has been reporting since 2013.
        /// </para>
        /// </remarks>
        /// <param name="s">A string.</param>
        /// <returns>An unkeyed 64-bit hash of <paramref name="s"/>, or <c>0</c> if <paramref name="s"/> is <c>null</c>.</returns>
        public static long ComputeStringHash64( string? s )
        {
            if ( s == null )
            {
                return 0;
            }

            s = s.Trim().ToLowerInvariant().Normalize();

            var bytes = Encoding.UTF8.GetBytes( s );

            byte[] hash;

#pragma warning disable CA5351 // MD5 is required to produce the same values as PostSharp.
            using ( var md5 = MD5.Create() )
            {
                hash = md5.ComputeHash( bytes );
            }
#pragma warning restore CA5351

            // Read the first eight bytes as a little-endian signed integer. The bytes are combined explicitly instead
            // of being reinterpreted, so that the value does not depend on the endianness of the platform.
            long hash64 = 0;

            for ( var i = 7; i >= 0; i-- )
            {
                hash64 = (hash64 << 8) | hash[i];
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