// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;
#if TEST_OPTIONS
// @RemoveOutputCode
#endif

namespace Metalama.Patterns.Caching.AspectTests.InvalidateCacheAttributeTests.Diagnostics;

public static class MethodWithNotMatchingParameterName
{
    // ReSharper disable once MemberCanBePrivate.Global
    public sealed class CachingClass
    {
        [Cache( IgnoreThisParameter = true )]
        public object DoAction( int notMatchingParameter )
        {
            return null!;
        }
    }

    public class InvalidatingClass
    {
        [InvalidateCache( typeof(CachingClass), nameof(CachingClass.DoAction) )]
        public void Invalidate( int param ) { }
    }
}