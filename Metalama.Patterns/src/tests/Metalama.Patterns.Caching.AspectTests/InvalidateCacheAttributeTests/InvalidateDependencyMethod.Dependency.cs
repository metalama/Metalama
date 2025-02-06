// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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