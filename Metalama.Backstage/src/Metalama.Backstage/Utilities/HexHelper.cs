// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Text;

namespace Metalama.Backstage.Utilities
{
    internal static class HexHelper
    {
        private static readonly char[] _hexChars =
            new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        /// <summary>
        /// Formats an array of bytes into an hexadecimal string, using a <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="bytes">An array of bytes.</param>
        /// <param name="builder">The <see cref="StringBuilder"/> into which the string has to be written.</param>
        private static void FormatBytes( byte[] bytes, StringBuilder builder )
        {
            if ( bytes is { Length: > 0 } )
            {
                var finalSize = builder.Length + (bytes.Length * 2);

                if ( builder.Capacity < finalSize )
                {
                    builder.Capacity = finalSize * 2;
                }

                foreach ( var b in bytes )
                {
                    builder.Append( _hexChars[b >> 4] );
                    builder.Append( _hexChars[b & 0xf] );
                }
            }
            else
            {
                builder.Append( "null" );
            }
        }

        /// <summary>
        /// Formats an array of bytes into a hexadecimal string.
        /// </summary>
        /// <param name="bytes">An array of bytes.</param>
        /// <returns>The hexadecimal string corresponding to <paramref name="bytes"/>.</returns>
        public static string FormatBytes( byte[] bytes, string nullString = "null" )
        {
            if ( bytes.Length == 0 )
            {
                return nullString;
            }

            var builder = new StringBuilder( 20 );
            FormatBytes( bytes, builder );

            return builder.ToString();
        }
    }
}