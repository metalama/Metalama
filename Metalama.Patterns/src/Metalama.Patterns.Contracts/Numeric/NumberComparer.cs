// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Contracts.Numeric;

[PublicAPI]
public static class NumberComparer
{
    public static bool? IsGreaterThan( object? o, long min )
        => o switch
        {
            sbyte sbyteValue => sbyteValue >= min,
            short shortValue => shortValue >= min,
            int intValue => intValue >= min,
            long longValue => longValue >= min,
            byte byteValue => byteValue >= min,
            ushort ushortValue => ushortValue >= min,
            uint uintValue => uintValue >= min,
            ulong ulongValue => ulongValue > long.MaxValue || (long) ulongValue >= min,
            double doubleValue => !(doubleValue < long.MinValue) && (doubleValue > long.MaxValue || (long) doubleValue >= min),
            float floatValue => !(floatValue < long.MinValue) && (floatValue > long.MaxValue || (long) floatValue >= min),
            decimal decimalValue => !(decimalValue < long.MinValue) && (decimalValue > long.MaxValue || (long) decimalValue >= min),
            _ => null
        };

    public static bool? IsStrictlyGreaterThan( object? o, long min )
        => o switch
        {
            sbyte sbyteValue => sbyteValue > min,
            short shortValue => shortValue > min,
            int intValue => intValue > min,
            long longValue => longValue > min,
            byte byteValue => byteValue > min,
            ushort ushortValue => ushortValue > min,
            uint uintValue => uintValue > min,
            ulong ulongValue => ulongValue > long.MaxValue || (long) ulongValue > min,
            double doubleValue => !(doubleValue < long.MinValue) && (doubleValue > long.MaxValue || (long) doubleValue > min),
            float floatValue => !(floatValue < long.MinValue) && (floatValue > long.MaxValue || (long) floatValue > min),
            decimal decimalValue => !(decimalValue < long.MinValue) && (decimalValue > long.MaxValue || (long) decimalValue > min),
            _ => null
        };

    public static bool? IsGreaterThan( object? o, ulong min )
        => o switch
        {
            sbyte sbyteValue => sbyteValue >= 0 && (ulong) sbyteValue >= min,
            short shortValue => shortValue >= 0 && (ulong) shortValue >= min,
            int intValue => intValue >= 0 && (ulong) intValue >= min,
            long longValue => longValue >= 0 && (ulong) longValue >= min,
            byte byteValue => byteValue >= min,
            ushort ushortValue => ushortValue >= min,
            uint uintValue => uintValue >= min,
            ulong ulongValue => ulongValue >= min,
            double doubleValue => !(doubleValue < ulong.MinValue) && (doubleValue > ulong.MaxValue || (ulong) doubleValue >= min),
            float floatValue => !(floatValue < ulong.MinValue) && (floatValue > ulong.MaxValue || (ulong) floatValue >= min),
            decimal decimalValue => !(decimalValue < ulong.MinValue) && (decimalValue > ulong.MaxValue || (ulong) decimalValue >= min),
            _ => null
        };

    public static bool? IsStrictlyGreaterThan( object? o, ulong min )
        => o switch
        {
            sbyte sbyteValue => sbyteValue >= 0 && (ulong) sbyteValue > min,
            short shortValue => shortValue >= 0 && (ulong) shortValue > min,
            int intValue => intValue >= 0 && (ulong) intValue > min,
            long longValue => longValue >= 0 && (ulong) longValue > min,
            byte byteValue => byteValue > min,
            ushort ushortValue => ushortValue > min,
            uint uintValue => uintValue > min,
            ulong ulongValue => ulongValue > min,
            double doubleValue => !(doubleValue < ulong.MinValue) && (doubleValue > ulong.MaxValue || (ulong) doubleValue > min),
            float floatValue => !(floatValue < ulong.MinValue) && (floatValue > ulong.MaxValue || (ulong) floatValue > min),
            decimal decimalValue => !(decimalValue < ulong.MinValue) && (decimalValue > ulong.MaxValue || (ulong) decimalValue > min),
            _ => null
        };

    public static bool? IsGreaterThan( object? o, double min )
        => o switch
        {
            sbyte sbyteValue => sbyteValue >= min,
            short shortValue => shortValue >= min,
            int intValue => intValue >= min,
            long longValue => longValue >= min,
            byte byteValue => byteValue >= min,
            ushort ushortValue => ushortValue >= min,
            uint uintValue => uintValue >= min,
            ulong ulongValue => ulongValue >= min,
            double doubleValue => doubleValue >= min,
            float floatValue => floatValue >= min,
            decimal decimalValue => (double) decimalValue >= min,
            _ => null
        };

    public static bool? IsStrictlyGreaterThan( object? o, double min )
        => o switch
        {
            sbyte sbyteValue => sbyteValue > min,
            short shortValue => shortValue > min,
            int intValue => intValue > min,
            long longValue => longValue > min,
            byte byteValue => byteValue > min,
            ushort ushortValue => ushortValue > min,
            uint uintValue => uintValue > min,
            ulong ulongValue => ulongValue > min,
            double doubleValue => doubleValue > min,
            float floatValue => floatValue > min,
            decimal decimalValue => (double) decimalValue > min,
            _ => null
        };

    public static bool? IsGreaterThan( object? o, decimal min )
        => o switch
        {
            sbyte sbyteValue => sbyteValue >= min,
            short shortValue => shortValue >= min,
            int intValue => intValue >= min,
            long longValue => longValue >= min,
            byte byteValue => byteValue >= min,
            ushort ushortValue => ushortValue >= min,
            uint uintValue => uintValue >= min,
            ulong ulongValue => ulongValue >= min,
            double doubleValue => doubleValue >= (double) decimal.MinValue && (doubleValue >= (double) decimal.MaxValue || (decimal) doubleValue >= min),
            float floatValue => floatValue >= (float) decimal.MinValue && (floatValue >= (double) decimal.MaxValue || (decimal) floatValue >= min),
            decimal decimalValue => decimalValue >= min,
            _ => null
        };

    public static bool? IsStrictlyGreaterThan( object? o, decimal min )
        => o switch
        {
            sbyte sbyteValue => sbyteValue > min,
            short shortValue => shortValue > min,
            int intValue => intValue > min,
            long longValue => longValue > min,
            byte byteValue => byteValue > min,
            ushort ushortValue => ushortValue > min,
            uint uintValue => uintValue > min,
            ulong ulongValue => ulongValue > min,
            double doubleValue => doubleValue > (double) decimal.MinValue && (doubleValue > (double) decimal.MaxValue || (decimal) doubleValue > min),
            float floatValue => floatValue > (float) decimal.MinValue && (floatValue > (double) decimal.MaxValue || (decimal) floatValue > min),
            decimal decimalValue => decimalValue > min,
            _ => null
        };

    // ---
    public static bool? IsSmallerThan( object? o, long max )
        => o switch
        {
            sbyte sbyteValue => sbyteValue <= max,
            short shortValue => shortValue <= max,
            int intValue => intValue <= max,
            long longValue => longValue <= max,
            byte byteValue => byteValue <= max,
            ushort ushortValue => ushortValue <= max,
            uint uintValue => uintValue <= max,
            ulong ulongValue => ulongValue <= long.MaxValue && (long) ulongValue <= max,
            double doubleValue => doubleValue <= long.MinValue || (doubleValue <= long.MaxValue && (long) doubleValue <= max),
            float floatValue => floatValue <= long.MinValue || (floatValue <= long.MaxValue && (long) floatValue <= max),
            decimal decimalValue => decimalValue <= long.MinValue || (decimalValue <= long.MaxValue && (long) decimalValue <= max),
            _ => null
        };

    public static bool? IsStrictlySmallerThan( object? o, long max )
        => o switch
        {
            sbyte sbyteValue => sbyteValue < max,
            short shortValue => shortValue < max,
            int intValue => intValue < max,
            long longValue => longValue < max,
            byte byteValue => byteValue < max,
            ushort ushortValue => ushortValue < max,
            uint uintValue => uintValue < max,
            ulong ulongValue => ulongValue <= long.MaxValue && (long) ulongValue < max,
            double doubleValue => doubleValue <= long.MinValue || (doubleValue <= long.MaxValue && (long) doubleValue < max),
            float floatValue => floatValue <= long.MinValue || (floatValue <= long.MaxValue && (long) floatValue < max),
            decimal decimalValue => decimalValue <= long.MinValue || (decimalValue <= long.MaxValue && (long) decimalValue < max),
            _ => null
        };

    public static bool? IsSmallerThan( object? o, ulong max )
        => o switch
        {
            sbyte sbyteValue => sbyteValue <= 0 || (ulong) sbyteValue <= max,
            short shortValue => shortValue <= 0 || (ulong) shortValue <= max,
            int intValue => intValue <= 0 || (ulong) intValue <= max,
            long longValue => longValue <= 0 || (ulong) longValue <= max,
            byte byteValue => byteValue <= max,
            ushort ushortValue => ushortValue <= max,
            uint uintValue => uintValue <= max,
            ulong ulongValue => ulongValue <= max,
            double doubleValue => doubleValue <= ulong.MinValue || (doubleValue <= ulong.MaxValue && (ulong) doubleValue <= max),
            float floatValue => floatValue <= ulong.MinValue || (floatValue <= ulong.MaxValue && (ulong) floatValue <= max),
            decimal decimalValue => decimalValue <= ulong.MinValue || (decimalValue <= ulong.MaxValue && (ulong) decimalValue <= max),
            _ => null
        };

    public static bool? IsStrictlySmallerThan( object? o, ulong max )
        => o switch
        {
            sbyte sbyteValue => sbyteValue < 0 || (ulong) sbyteValue < max,
            short shortValue => shortValue < 0 || (ulong) shortValue < max,
            int intValue => intValue < 0 || (ulong) intValue < max,
            long longValue => longValue < 0 || (ulong) longValue < max,
            byte byteValue => byteValue < max,
            ushort ushortValue => ushortValue < max,
            uint uintValue => uintValue < max,
            ulong ulongValue => ulongValue < max,
            double doubleValue => doubleValue < ulong.MinValue || (doubleValue <= ulong.MaxValue && (ulong) doubleValue < max),
            float floatValue => floatValue < ulong.MinValue || (floatValue <= ulong.MaxValue && (ulong) floatValue < max),
            decimal decimalValue => decimalValue < ulong.MinValue || (decimalValue <= ulong.MaxValue && (ulong) decimalValue < max),
            _ => null
        };

    public static bool? IsSmallerThan( object? o, double max )
        => o switch
        {
            sbyte sbyteValue => sbyteValue <= max,
            short shortValue => shortValue <= max,
            int intValue => intValue <= max,
            long longValue => longValue <= max,
            byte byteValue => byteValue <= max,
            ushort ushortValue => ushortValue <= max,
            uint uintValue => uintValue <= max,
            ulong ulongValue => ulongValue <= max,
            double doubleValue => doubleValue <= max,
            float floatValue => floatValue <= max,
            decimal decimalValue => (double) decimalValue <= max,
            _ => null
        };

    public static bool? IsStrictlySmallerThan( object? o, double max )
        => o switch
        {
            sbyte sbyteValue => sbyteValue < max,
            short shortValue => shortValue < max,
            int intValue => intValue < max,
            long longValue => longValue < max,
            byte byteValue => byteValue < max,
            ushort ushortValue => ushortValue < max,
            uint uintValue => uintValue < max,
            ulong ulongValue => ulongValue < max,
            double doubleValue => doubleValue < max,
            float floatValue => floatValue < max,
            decimal decimalValue => (double) decimalValue < max,
            _ => null
        };

    public static bool? IsSmallerThan( object? o, decimal max )
        => o switch
        {
            sbyte sbyteValue => sbyteValue <= max,
            short shortValue => shortValue <= max,
            int intValue => intValue <= max,
            long longValue => longValue <= max,
            byte byteValue => byteValue <= max,
            ushort ushortValue => ushortValue <= max,
            uint uintValue => uintValue <= max,
            ulong ulongValue => ulongValue <= max,
            double doubleValue => doubleValue <= (double) decimal.MinValue || (doubleValue <= (double) decimal.MaxValue && (decimal) doubleValue <= max),
            float floatValue => floatValue <= (double) decimal.MinValue || (floatValue <= (double) decimal.MaxValue && (decimal) floatValue <= max),
            decimal decimalValue => decimalValue <= max,
            _ => null
        };

    public static bool? IsStrictlySmallerThan( object? o, decimal max )
        => o switch
        {
            sbyte sbyteValue => sbyteValue < max,
            short shortValue => shortValue < max,
            int intValue => intValue < max,
            long longValue => longValue < max,
            byte byteValue => byteValue < max,
            ushort ushortValue => ushortValue < max,
            uint uintValue => uintValue < max,
            ulong ulongValue => ulongValue < max,
            double doubleValue => doubleValue <= (double) decimal.MinValue || (doubleValue <= (double) decimal.MaxValue && (decimal) doubleValue < max),
            float floatValue => floatValue <= (double) decimal.MinValue || (floatValue <= (double) decimal.MaxValue && (decimal) floatValue < max),
            decimal decimalValue => decimalValue < max,
            _ => null
        };
}