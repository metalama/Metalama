// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Caching.Backends;

public sealed record LayeredCachingBackendConfiguration : CachingBackendConfiguration
{
    public MemoryCachingBackendConfiguration? L1Configuration { get; init; }
}