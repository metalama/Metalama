// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;
#if TEST_OPTIONS
// @RemoveOutputCode
#endif

namespace Metalama.Patterns.Caching.AspectTests.CacheAttributeTests.Diagnostics;

public class OutParameter
{
    [Cache]
    public int Test( out int a )
    {
        a = 8;

        return 42;
    }
}