// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;

namespace Metalama.Patterns.Caching.AspectTests.CacheAttributeTests.Static;

[CachingConfiguration( UseDependencyInjection = false )]
public class C
{
    [Cache]
    public static int M() => 5;
}