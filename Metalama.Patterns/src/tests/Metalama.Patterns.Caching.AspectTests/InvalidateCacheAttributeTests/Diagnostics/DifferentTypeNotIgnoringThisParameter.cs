// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;
#if TEST_OPTIONS
// @RemoveOutputCode
#endif

namespace Metalama.Patterns.Caching.AspectTests.InvalidateCacheAttributeTests.Diagnostics;

public static class DifferentTypeNotIgnoringThisParameter
{
    public sealed class CachingClass
    {
        [Cache]
        public object? DoAction()
        {
            return null;
        }
    }

    public sealed class InvalidatingClass
    {
        [InvalidateCache( typeof(CachingClass), nameof(CachingClass.DoAction) )]
        public void Invalidate() { }
    }
}