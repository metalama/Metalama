// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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