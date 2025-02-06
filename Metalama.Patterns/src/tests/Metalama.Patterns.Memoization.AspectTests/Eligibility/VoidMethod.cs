// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Memoization.AspectTests.Eligibility.VoidMethod;

public class C
{
    [Memoize]
    private void M() { }
}