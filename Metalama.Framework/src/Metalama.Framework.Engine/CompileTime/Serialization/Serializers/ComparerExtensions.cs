// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers
{
    internal static class ComparerExtensions
    {
        public static byte GetComparerCode<T>( IEqualityComparer<T> comparer )
        {
            if ( Equals( comparer, EqualityComparer<T>.Default ) )
            {
                return 0;
            }

            if ( Equals( comparer, StringComparer.CurrentCulture ) )
            {
                return 1;
            }

            if ( Equals( comparer, StringComparer.CurrentCultureIgnoreCase ) )
            {
                return 2;
            }

            if ( Equals( comparer, StringComparer.Ordinal ) )
            {
                return 3;
            }

            if ( Equals( comparer, StringComparer.OrdinalIgnoreCase ) )
            {
                return 4;
            }

            return byte.MaxValue;
        }

        public static IEqualityComparer<string>? GetComparerFromCode( byte code )
        {
            switch ( code )
            {
                case 1:
                    return StringComparer.CurrentCulture;

                case 2:
                    return StringComparer.CurrentCultureIgnoreCase;

                case 3:
                    return StringComparer.Ordinal;

                case 4:
                    return StringComparer.OrdinalIgnoreCase;

                default:
                    return null;
            }
        }
    }
}