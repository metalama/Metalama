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
    internal static class Int64Minimum
    {
        public static long ToInt64( long min )
        {
            if ( min == long.MaxValue )
            {
                return long.MaxValue;
            }

            return min + 1;
        }

        public static ulong ToUInt64( long min )
        {
            if ( min < 0 )
            {
                return 0;
            }

            return (ulong) min + 1;
        }

        public static double ToDouble( long min ) => (double) min + GetDoubleStep( (double) min );

        public static decimal ToDecimal( long min ) => (decimal) min + GetDecimalStep( (decimal) min );
    }
}