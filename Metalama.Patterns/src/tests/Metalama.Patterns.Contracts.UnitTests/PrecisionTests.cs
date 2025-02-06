// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Globalization;
using Xunit;

namespace Metalama.Patterns.Contracts.UnitTests;

public sealed class PrecisionTests : RangeContractTestsBase
{
    [Fact]
    public void CheckDoubleTolerance()
        => Assert.Equal(
            DoubleTolerance.ToString( CultureInfo.InvariantCulture ),
            Utilities.FloatingPointHelper.DoubleTolerance.ToString( CultureInfo.InvariantCulture ) );

    [Fact]
    public void CheckDecimalTolerance() => Assert.Equal( DecimalTolerance, Utilities.FloatingPointHelper.DecimalTolerance );
}