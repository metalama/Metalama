// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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