// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Contracts.AspectTests.Diagnostics;

public class Range_DecimalStrictlyGreaterThanDouble
{
    // ReSharper disable once MissingLinebreak
    public void MethodWithDecimalStrictlyGreaterThanDouble(
        [StrictlyGreaterThan( (double) decimal.MaxValue + ((double) decimal.MaxValue * 1e-6) )] decimal? a ) { }
}