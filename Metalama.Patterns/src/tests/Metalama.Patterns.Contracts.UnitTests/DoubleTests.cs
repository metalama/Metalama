// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if NET6_0
using Xunit;
#endif

namespace Metalama.Patterns.Contracts.UnitTests;

public class DoubleTests
{
#if NET6_0
    [Fact]
    public void StrictlyLessThan()
    {
        _ = SlightlyLessThanOne();
    }

    [return: StrictlyLessThan( 1 )]
    private static double SlightlyLessThanOne()
    {
        var result = Math.BitDecrement( 1 ); // 0.9999999999999999
        Assert.True( result < 1 );

        return result;
    }

    [Fact]
    public void StrictlyGreaterThan()
    {
        _ = SlightlyMoreThanOne();
    }

    [return: StrictlyGreaterThan( 1 )]
    private static double SlightlyMoreThanOne()
    {
        var result = Math.BitIncrement( 1 );
        Assert.True( result > 1 );

        return result;
    }
#endif
}