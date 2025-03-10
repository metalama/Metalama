// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Backstage.Licensing.Licenses.LicenseFields
{
    internal static class LicenseFieldsExtensions
    {
        /// <summary>
        /// Returns <c>true</c> if a license field must be understood.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <remarks>
        /// Till PostSharp 6.5.16/6.8.9/6.9.2, all license fields had to be understood.
        /// Adding a field used to cause backward incompatibility.
        /// In the next versions and in Metalama, we allow fields of indexes 129-253
        /// to be followed by its length (1 byte).
        /// If such field is unknown, the given number of bytes is ignored.
        /// </remarks>
        public static bool IsMustUnderstand( this LicenseFieldIndex index )
        {
            var i = (byte) index;

            if ( i < 1 )
            {
                throw new ArgumentOutOfRangeException( nameof(index) );
            }

            return i is <= 128 or >= 254;
        }

        /// <summary>
        /// Returns <c>true</c> if a license field data contain its length.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <remarks>
        /// Till PostSharp 6.5.16/6.8.9/6.9.2, all license fields had to be known.
        /// Adding a field used to cause backward incompatibility.
        /// In the next versions and in Metalama, we require each new filed index
        /// to be followed by its length (1 byte).
        /// If such field is unknown, the given number of bytes is ignored.
        /// </remarks>
        public static bool IsPrefixedByLength( this LicenseFieldIndex index )
        {
            var i = (byte) index;

            if ( i < 1 )
            {
                throw new ArgumentOutOfRangeException( nameof(index) );
            }

            return i is > 21 and < 254;
        }
    }
}