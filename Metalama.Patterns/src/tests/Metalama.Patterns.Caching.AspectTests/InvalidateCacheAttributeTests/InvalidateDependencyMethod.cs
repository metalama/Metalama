// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Aspects;
#if TEST_OPTIONS
// @RemoveOutputCode
#endif

// ReSharper disable once CheckNamespace

namespace Metalama.Patterns.Caching.AspectTests.InvalidateCacheAttributeTests.InvalidateDependencyMethod;

public class TestClass
{
    /* At compile time, the DeclarationEnhancements collection obtained from method.Enhancements()
     * is only meaningful for declarations in the assembly being compiled. It will always be empty
     * for declarations in dependency assemblies. If cross-assembly aspect awareness is required,
     * it is up to the user to decide how to do this for their particular use case. For caching,
     * the InvalidateCacheAttribute aspect must look at the Attributes collection on the dependency
     * method to determine if it has been cached. This test verifies that behaviour.
     */
    [InvalidateCache( typeof(DependencyClass), nameof(DependencyClass.CachedUsingAttribute) )]
    public void InvalidateMethodCachedUsingAttribute() { }

    /* When the CacheAttribute aspect is applied by a fabric purely as an aspect, the Metalama framework
     * does not implicitly introduce a [Cache] attribute to the target method. Note that aspects do not
     * have to be attributes. To make this work as desired for caching, the CacheAttribute aspect
     * explicitly introduces a corresponding [Cache] attribute to its target if it does not have one
     * already. This test verifies that behaviour.
     */
    [InvalidateCache( typeof(DependencyClass), nameof(DependencyClass.CachedUsingFabric) )]
    public void InvalidateMethodCachedUsingFabric() { }
}