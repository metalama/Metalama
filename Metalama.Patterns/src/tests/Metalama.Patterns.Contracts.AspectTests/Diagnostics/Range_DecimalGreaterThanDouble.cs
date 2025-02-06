// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Contracts.AspectTests.Diagnostics;

public class Range_DecimalGreaterThanDouble
{
    public void MethodWithDecimalGreaterThanDouble( [GreaterThanOrEqual( (double) decimal.MaxValue + ((double) decimal.MaxValue * 1e-6) )] decimal? a ) { }
}