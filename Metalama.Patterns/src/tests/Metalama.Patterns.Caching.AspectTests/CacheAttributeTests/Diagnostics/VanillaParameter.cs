// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;
#if TEST_OPTIONS
// @RemoveOutputCode
#endif

namespace Metalama.Patterns.Caching.AspectTests.CacheAttributeTests.Diagnostics;

public class VanillaParameter
{
    [Cache]
    public int Test( int a ) => 42;
}