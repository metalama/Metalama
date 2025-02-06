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
    internal static class Int64Maximum
    {
        public static long ToInt64( long max )
        {
            if ( max == long.MinValue )
            {
                return long.MinValue;
            }

            return max - 1;
        }

        public static ulong ToUInt64( long max )
        {
            if ( max <= 0 )
            {
                return 0;
            }

            return (ulong) max - 1;
        }

        public static double ToDouble( long max ) => (double) max - GetDoubleStep( (double) max );

        public static decimal ToDecimal( long max ) => (decimal) max - GetDecimalStep( (decimal) max );
    }
}