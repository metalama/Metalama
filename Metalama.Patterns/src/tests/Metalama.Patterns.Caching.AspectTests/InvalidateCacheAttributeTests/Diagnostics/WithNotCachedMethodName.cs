// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;
#if TEST_OPTIONS
// @RemoveOutputCode
#endif

namespace Metalama.Patterns.Caching.AspectTests.InvalidateCacheAttributeTests.Diagnostics;

public static class WithNotCachedMethodName
{
    public class CachingClass
    {
        public object DoAction()
        {
            return null!;
        }

        [InvalidateCache( nameof(DoAction) )]
        public void Invalidate() { }
    }
}