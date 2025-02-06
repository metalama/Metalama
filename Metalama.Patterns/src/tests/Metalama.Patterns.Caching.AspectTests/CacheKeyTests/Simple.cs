// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;

namespace Metalama.Patterns.Caching.AspectTests.CacheKeyTests.Simple;

public class TheClass
{
    [CacheKey]
    public string Id { get; }

    [CacheKey]
    public int SubId { get; }

    public string? Description { get; }
}