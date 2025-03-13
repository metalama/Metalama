// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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