// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Contracts.AspectTests.Diagnostics;

public class Range_UlongGreaterThanDouble
{
    private void MethodWithUlongGreaterThanDouble( [GreaterThanOrEqual( (double) ulong.MaxValue + ((double) ulong.MaxValue * 1e-6) )] ulong? a ) { }
}