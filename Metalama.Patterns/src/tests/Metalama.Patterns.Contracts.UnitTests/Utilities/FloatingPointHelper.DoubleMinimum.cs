// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable IDE0004 // Remove Unnecessary Cast: in this problem domain, explicit casts add clarity.

// Resharper disable RedundantCast

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Contracts.UnitTests.Utilities;

internal partial class FloatingPointHelper
{
    [RunTimeOrCompileTime]
    [UsedImplicitly( ImplicitUseTargetFlags.WithMembers )]
    internal static class DoubleMinimum
    {
        public static long ToInt64( double min )
        {
            if ( min < (double) long.MinValue )
            {
                return long.MinValue;
            }

            if ( min + 1 > (double) long.MaxValue )
            {
                return long.MaxValue;
            }

            return (long) min + 1;
        }

        public static ulong ToUInt64( double min )
        {
            if ( min < 0 )
            {
                return 0;
            }

            if ( min + 1 > (double) ulong.MaxValue )
            {
                return ulong.MaxValue;
            }

            return (ulong) min + 1;
        }

        public static double ToDouble( double min )
        {
            if ( Math.Abs( min ) <= double.Epsilon )
            {
                return double.Epsilon;
            }

            var step = GetDoubleStep( min );

            if ( min >= double.MaxValue - step )
            {
                return double.MaxValue;
            }

            return min + step;
        }

        public static decimal ToDecimal( double min )
        {
            if ( min > (double) decimal.MaxValue )
            {
                return decimal.MaxValue;
            }

            if ( min < (double) decimal.MinValue )
            {
                return decimal.MinValue;
            }

            var step = GetDecimalStep( (decimal) min );

            if ( min > (double) decimal.MaxValue - (double) step )
            {
                return decimal.MaxValue;
            }

            if ( Math.Abs( min ) <= (double) step )
            {
                return step;
            }

            return (decimal) min + step;
        }
    }
}