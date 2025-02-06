// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Contracts.AspectTests.Diagnostics;

public class Range_DecimalStrictlyLessThanDouble
{
    public void MethodWithDecimalStrictlyLessThanDouble( [StrictlyLessThan( (double) decimal.MinValue + ((double) decimal.MinValue * 1e-6) )] decimal? a ) { }
}