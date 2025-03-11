// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Aspects;

namespace Metalama.Patterns.Caching.AspectTests.InvalidateCacheAttributeTests.ParameterMapping_Single;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

// <target>
internal class Target
{
    [Cache]
    public async Task<string?> GetResourceNameAsync( Guid resourceId ) { return "42"; }

    [InvalidateCache( nameof(GetResourceNameAsync) )]
    public async Task<ProtectedResource?> UpdateProtectedResourceAsync( Guid resourceId, UpdateProtectedResource update ) { return new ProtectedResource(); }

    [InvalidateCache( nameof(GetResourceNameAsync) )]
    public async Task<ProtectedResource?> UpdateProtectedResource2Async( UpdateProtectedResource update, Guid resourceId ) { return new ProtectedResource(); }
}

internal class ProtectedResource { }

internal class UpdateProtectedResource { }