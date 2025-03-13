// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RemoveOutputCode
#endif

using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.TestHelpers;

namespace Metalama.Patterns.Caching.AspectTests.InvalidateCacheAttributeTests.Diagnostics;

public static class MethodWithNotMatchingParameterType
{
    public sealed class CachingClass
    {
        [Cache( IgnoreThisParameter = true )]
        public object DoAction( CachedValueChildClass param )
        {
            return null!;
        }
    }

    public class InvalidatingClass
    {
        [InvalidateCache( typeof(CachingClass), nameof(CachingClass.DoAction) )]
        public void Invalidate( CachedValueClass param ) { }
    }
}