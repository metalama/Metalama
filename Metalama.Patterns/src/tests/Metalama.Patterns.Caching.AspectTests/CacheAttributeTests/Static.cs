// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Aspects;

namespace Metalama.Patterns.Caching.AspectTests.CacheAttributeTests.Static;

[CachingConfiguration( UseDependencyInjection = false )]
public class C
{
    [Cache]
    public static int M() => 5;
}