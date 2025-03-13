// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using Metalama.Patterns.Caching.Aspects;

// ReSharper disable once CheckNamespace
namespace Metalama.Patterns.Caching.AspectTests.InvalidateCacheAttributeTests.InvalidateDependencyMethod;

#pragma warning disable SA1101

public sealed class DependencyClass
{
    [Cache( IgnoreThisParameter = true )]
    public int CachedUsingAttribute() => 42;

    public int CachedUsingFabric() => 42;

    private sealed class Fabric : TypeFabric
    {
        public override void AmendType( ITypeAmender amender )
        {
            // ReSharper disable once ArrangeThisQualifier
            amender
                .Select( t => t.Methods.Single( m => m.Name == nameof(CachedUsingFabric) ) )
                .AddAspect( m => new CacheAttribute() { IgnoreThisParameter = true } );
        }
    }
}