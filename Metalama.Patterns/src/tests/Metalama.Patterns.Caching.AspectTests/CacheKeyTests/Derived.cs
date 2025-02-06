// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;

namespace Metalama.Patterns.Caching.AspectTests.CacheKeyTests.Derived;

public class BaseClass
{
    [CacheKey]
    public string Id { get; }

    public string? Description { get; }
}

public class DerivedClass : BaseClass
{
    [CacheKey]
    public int SubId { get; }
}