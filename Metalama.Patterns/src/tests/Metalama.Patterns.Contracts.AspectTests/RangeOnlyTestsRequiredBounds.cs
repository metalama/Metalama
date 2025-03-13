// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Contracts.AspectTests;

public sealed class RangeOnlyTestsRequiredBounds
{
    public void Negative( [NonPositive] int x ) { }

    public void Positive( [NonNegative] int x ) { }

    public void StrictlyNegative( [StrictlyNegative] int x ) { }

    public void StrictlyPositive( [StrictlyPositive] int x ) { }

    public void LessThanInt( [LessThanOrEqual( 42 )] int x ) { }

    public void GreaterThanInt( [GreaterThanOrEqual( 42 )] int x ) { }

    public void RangeInt( [Range( 10, 20 )] int x ) { }

    public void StrictlyGreaterThanInt( [StrictlyGreaterThan( 42 )] int x ) { }

    public void StrictlyLessThanInt( [StrictlyLessThan( 42 )] int x ) { }

    public void StrictRangeInt( [StrictRange( 10.0, 20.0 )] int x ) { }

    public void LessThanDouble( [LessThanOrEqual( 42.0 )] int x ) { }

    public void GreaterThanDouble( [GreaterThanOrEqual( 42.0 )] int x ) { }

    public void RangeDouble( [Range( 10.0, 20.0 )] int x ) { }

    public void StrictlyGreaterThanDouble( [StrictlyGreaterThan( 42.0 )] int x ) { }

    public void StrictlyLessThanDouble( [StrictlyLessThan( 42.0 )] int x ) { }

    public void StrictRangeDouble( [StrictRange( 10.0, 20.0 )] int x ) { }

    public void LessThanUnsigned( [LessThanOrEqual( 42ul )] int x ) { }

    public void GreaterThanUnsigned( [GreaterThanOrEqual( 42ul )] int x ) { }

    public void RangeUnsigned( [Range( 10ul, 20ul )] int x ) { }

    public void StrictlyGreaterThanUnsigned( [StrictlyGreaterThan( 42ul )] int x ) { }

    public void StrictlyLessThanUnsigned( [StrictlyLessThan( 42ul )] int x ) { }

    public void StrictRangeUnsigned( [StrictRange( 10ul, 20ul )] int x ) { }
}