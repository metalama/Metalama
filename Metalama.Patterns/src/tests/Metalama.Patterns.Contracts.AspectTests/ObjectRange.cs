// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Contracts.AspectTests.ObjectRange;

internal class C
{
    [return: StrictlyPositive]
    public object M( [NonNegative] object a, [Range( 0, 100 )] object b, [LessThanOrEqual( 101, decimalPlaces: 2 )] out object c )
    {
        c = a;

        return b;
    }
}