// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Contracts.AspectTests.Diagnostics;

public class Range_LongGreaterThanUlong
{
    private void MethodWithLongGreaterThanUlong( [GreaterThanOrEqual( (ulong) long.MaxValue + 1 )] long? a ) { }
}