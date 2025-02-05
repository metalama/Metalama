// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using K4os.Hash.xxHash;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

#if NETCOREAPP2_1_OR_GREATER
using System;
#endif

namespace Metalama.Framework.Engine.Utilities
{
    [PublicAPI]
    public static class HashUtilities
    {
        public static string HashString( string s ) => XXH64.DigestOf( Encoding.UTF8.GetBytes( s ) ).ToString( "x16", CultureInfo.InvariantCulture );

        public static ulong HashStrings<T>( T strings )
            where T : IEnumerable<string>
        {
            var hash = new XXH64();

            foreach ( var s in strings )
            {
                hash.Update( s );
            }

            return hash.Digest();
        }

        public static void Update( this XXH64 hash, string? value )
        {
            if ( value == null )
            {
                hash.Update( 0 );
            }
            else
            {
#if NETCOREAPP2_1_OR_GREATER
                const int maxStackLimit = 1024;

                if ( Encoding.UTF8.GetMaxByteCount( value.Length ) <= maxStackLimit )
                {
                    Span<byte> bytes = stackalloc byte[maxStackLimit];

                    var encodedLength = Encoding.UTF8.GetBytes( value, bytes );

                    hash.Update( bytes[..encodedLength] );
                }
                else
                {
                    hash.Update( Encoding.UTF8.GetBytes( value ) );
                }
#else
                hash.Update( Encoding.UTF8.GetBytes( value ) );
#endif
            }
        }

        public static unsafe void Update<T>( this XXH64 hash, T value )
            where T : unmanaged
            => hash.Update( &value, sizeof(T) );

        public static unsafe void Update( this XXH64 hash, long value ) => hash.Update( &value, sizeof(long) );

        public static unsafe void Update( this XXH64 hash, ulong value ) => hash.Update( &value, sizeof(ulong) );

        // The following overloads are redundant but they work around a compiler bug.
        public static void Update( this XXH64 hash, ImmutableArray<byte> bytes ) => hash.Update( bytes.AsSpan() );

        public static unsafe void Update( this XXH64 hash, int value ) => hash.Update( &value, sizeof(int) );
    }
}