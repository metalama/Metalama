// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Memoization.AspectTests.Eligibility.Parameters;

public class C
{
    [Memoize]
    private int M( int x ) => x;
}