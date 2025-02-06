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
    internal static class UInt64Maximum
    {
        public static long ToInt64( ulong max )
        {
            if ( max > (ulong) long.MaxValue + 1 )
            {
                return long.MaxValue;
            }

            return (long) max - 1;
        }

        public static ulong ToUInt64( ulong max )
        {
            if ( max == 0 )
            {
                return 0;
            }

            return max - 1;
        }

        public static double ToDouble( ulong max ) => (double) max - GetDoubleStep( (double) max );

        public static decimal ToDecimal( ulong max ) => (decimal) max + GetDecimalStep( (decimal) max );
    }
}