// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Utilities.Caching;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO.Hashing;
using System.Text;

namespace Metalama.Framework.Engine.Utilities
{
    [PublicAPI]
    public static class HashUtilities
    {
        private static readonly ObjectPool<XxHash64> _hasherPool = new(
            () => new XxHash64(),
            trimOnFree: false );

        /// <summary>
        /// Gets a pooled XxHash64 instance. Dispose the handle to return it to the pool.
        /// The hasher is reset before being returned.
        /// </summary>
        public static ObjectPoolHandle<XxHash64> AllocateHasher()
        {
            var handle = _hasherPool.Allocate();
            handle.Value.Reset();

            return handle;
        }

        public static string HashString( string s ) => XxHash64.HashToUInt64( Encoding.UTF8.GetBytes( s ) ).ToString( "x16", CultureInfo.InvariantCulture );

        public static ulong HashStrings<T>( T strings )
            where T : IEnumerable<string>
        {
            using var handle = AllocateHasher();
            var hash = handle.Value;

            foreach ( var s in strings )
            {
                hash.Append( s );
            }

            return hash.GetCurrentHashAsUInt64();
        }

        public static void Append( this XxHash64 hash, string? value )
        {
            if ( value == null )
            {
                hash.Append( 0 );
            }
            else
            {
#if NETCOREAPP2_1_OR_GREATER
                const int maxStackLimit = 1024;

                if ( Encoding.UTF8.GetMaxByteCount( value.Length ) <= maxStackLimit )
                {
                    Span<byte> bytes = stackalloc byte[maxStackLimit];

                    var encodedLength = Encoding.UTF8.GetBytes( value, bytes );

                    hash.Append( bytes[..encodedLength] );
                }
                else
                {
                    hash.Append( Encoding.UTF8.GetBytes( value ) );
                }
#else
                hash.Append( Encoding.UTF8.GetBytes( value ) );
#endif
            }
        }

        public static unsafe void Append<T>( this XxHash64 hash, T value )
            where T : unmanaged
            => hash.Append( new ReadOnlySpan<byte>( &value, sizeof(T) ) );

        public static unsafe void Append( this XxHash64 hash, long value ) => hash.Append( new ReadOnlySpan<byte>( &value, sizeof(long) ) );

        public static unsafe void Append( this XxHash64 hash, ulong value ) => hash.Append( new ReadOnlySpan<byte>( &value, sizeof(ulong) ) );

        // The following overloads are redundant but they work around a compiler bug.
        public static void Append( this XxHash64 hash, ImmutableArray<byte> bytes ) => hash.Append( bytes.AsSpan() );

        public static unsafe void Append( this XxHash64 hash, int value ) => hash.Append( new ReadOnlySpan<byte>( &value, sizeof(int) ) );
    }
}