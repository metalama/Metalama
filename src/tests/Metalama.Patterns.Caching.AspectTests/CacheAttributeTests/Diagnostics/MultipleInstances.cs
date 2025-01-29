// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @RemoveOutputCode
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using Metalama.Patterns.Caching.Aspects;

namespace Metalama.Patterns.Caching.AspectTests.CacheAttributeTests.Diagnostics;

// ReSharper disable ArrangeThisQualifier
#pragma warning disable SA1101 // Prefix local calls with this

public class MultipleInstances
{
    [Cache]
    public int MethodWithAttribute() => 42;

    public int MethodWithoutAttribute() => 42;

    private sealed class Fabric : TypeFabric
    {
        public override void AmendType( ITypeAmender amender )
        {
            // Attribute plus aspect:
            amender
                .Select( t => t.Methods.Single( m => m.Name == nameof(MethodWithAttribute) ) )
                .AddAspect( m => new CacheAttribute() );

            // Two aspects:
            amender
                .Select( t => t.Methods.Single( m => m.Name == nameof(MethodWithoutAttribute) ) )
                .AddAspect( m => new CacheAttribute() );

            amender
                .Select( t => t.Methods.Single( m => m.Name == nameof(MethodWithoutAttribute) ) )
                .AddAspect( m => new CacheAttribute() );
        }
    }
}