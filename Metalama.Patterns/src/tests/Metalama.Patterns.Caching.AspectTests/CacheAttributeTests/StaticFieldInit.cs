// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Aspects;

#pragma warning disable IDE0052

namespace Metalama.Patterns.Caching.AspectTests.CacheAttributeTests.StaticFieldInit;

[CachingConfiguration( UseDependencyInjection = false )]
public class C
{
    private static readonly int _cachedValue = M();

    [Cache]
    public static int M() => 5;
}
