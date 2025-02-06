// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Metalama.Patterns.Caching.AspectTests.InvalidateCacheAttributeTests.ParameterMapping_Multiple;

// <target>
internal class Target
{
    [Cache]
    public string GetResourceName1( int x, int y, [NotCacheKey] int z ) => "resource";

    [Cache]
    public string GetResourceName2( int y, [NotCacheKey] string z, int x ) => "resource";

    [InvalidateCache( nameof(GetResourceName1) )]
    [InvalidateCache( nameof(GetResourceName2) )]
    public async Task<ProtectedResource?> UpdateProtectedResourceAsync( int x, int y, UpdateProtectedResource update ) { return new ProtectedResource(); }

    [InvalidateCache( nameof(GetResourceName1) )]
    [InvalidateCache( nameof(GetResourceName2) )]
    public async Task<ProtectedResource?> UpdateProtectedResource2Async( UpdateProtectedResource update, int y, int x ) { return new ProtectedResource(); }
}

internal class ProtectedResource { }

internal class UpdateProtectedResource { }