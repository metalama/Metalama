// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Contracts.UnitTests.Utilities;

[RunTimeOrCompileTime]
internal static partial class FloatingPointHelper
{
    public static readonly double DoubleTolerance = GetDoubleTolerance();
    public static readonly decimal DecimalTolerance = GetDecimalTolerance();

    public static double GetDoubleStep( double value )
    {
        value = Math.Abs( value );

        if ( value <= double.Epsilon )
        {
            return double.Epsilon;
        }

        return value * DoubleTolerance;
    }

    public static decimal GetDecimalStep( decimal value )
    {
        if ( value == 0 )
        {
            return DecimalTolerance;
        }

        return Math.Abs( value ) * DecimalTolerance;
    }

    private static double GetDoubleTolerance()
    {
        double value = 1;
        double lastGoodValue = 1;

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        while ( 1 + value != 1 )
        {
            lastGoodValue = value;
            value /= 2;
        }

        return lastGoodValue;
    }

    private static decimal GetDecimalTolerance()
    {
        decimal value = 1;
        decimal lastGoodValue = 1;

        while ( 1 + value != 1 )
        {
            lastGoodValue = value;
            value /= 10;
        }

        return lastGoodValue;
    }
}