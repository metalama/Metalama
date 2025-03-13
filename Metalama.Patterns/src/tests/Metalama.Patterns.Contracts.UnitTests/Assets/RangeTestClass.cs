// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Contracts.UnitTests.Assets;

#pragma warning disable LAMA5006 // Intentionally with redundant checks.
public class RangeTestClass
{
    [GreaterThan( 0 )]
    public int GreaterThanZeroField;

    [LessThan( 0 )]
    public long LessThanZeroField { get; set; }

    public int ZeroToTenMethod( [Range( 0, 10 )] short parameter ) => parameter;

    public double ZeroToTenDouble( [Range( 0d, 10d )] double parameter ) => parameter;

    public decimal ZeroToTenDecimal( [Range( 0d, 10d )] decimal parameter ) => parameter;

    public decimal? ZeroToTenNullableDecimal( [Range( 0d, 10d )] decimal? parameter ) => parameter;

    public long? ZeroToTenNullableInt( [Range( 0, 10 )] long? parameter ) => parameter;

    public float ZeroToTenFloat( [Range( 0, 10 )] float parameter ) => parameter;

    public float? ZeroToTenNullableFloat( [Range( 0, 10 )] float? parameter ) => parameter;

    public decimal LargeDecimalRange( [Range( double.MinValue, double.MaxValue )] decimal parameter ) => parameter;

    public void ZeroToTenNullableIntRef( long? newVal, [Range( 0, 10 )] ref long? parameter ) => parameter = newVal;

    public void ZeroToTenNullableIntOut( long? newVal, [Range( 0, 10 )] out long? parameter ) => parameter = newVal;

    [return: Range( 0, 10 )]
    public long? ZeroToTenNullableIntRetVal( long? retVal ) => retVal;
}