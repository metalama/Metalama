// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#pragma warning disable IDE0004 // Remove Unnecessary Cast: in this problem domain, explicit casts add clarity.

// Resharper disable RedundantCast

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Contracts.UnitTests.Utilities;

internal partial class FloatingPointHelper
{
    [RunTimeOrCompileTime]
    [UsedImplicitly( ImplicitUseTargetFlags.WithMembers )]
    internal static class UInt64Minimum
    {
        public static long ToInt64( ulong min )
        {
            if ( min > (ulong) long.MaxValue + 1 )
            {
                return long.MaxValue;
            }

            return (long) min + 1;
        }

        public static ulong ToUInt64( ulong min )
        {
            if ( min == ulong.MaxValue )
            {
                return ulong.MaxValue;
            }

            return min + 1;
        }

        public static double ToDouble( ulong min ) => (double) min + GetDoubleStep( (double) min );

        public static decimal ToDecimal( ulong min ) => (decimal) min + GetDecimalStep( (decimal) min );
    }
}