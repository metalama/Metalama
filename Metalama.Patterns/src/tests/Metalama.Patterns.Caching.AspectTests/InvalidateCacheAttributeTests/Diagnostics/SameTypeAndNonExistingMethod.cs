// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Aspects;
#if TEST_OPTIONS
// @RemoveOutputCode
#endif

namespace Metalama.Patterns.Caching.AspectTests.InvalidateCacheAttributeTests.Diagnostics;

public static class SameTypeAndNonExistingMethod
{
    public class CachingClass
    {
        [Cache]
        public object DoAction()
        {
            return null!;
        }

        [InvalidateCache( "NonExistingMethod" )]
        public void Invalidate() { }
    }
}