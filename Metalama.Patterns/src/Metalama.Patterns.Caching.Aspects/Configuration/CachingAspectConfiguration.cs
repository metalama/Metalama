// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Caching.Aspects.Configuration;

[PublicAPI]
[CompileTime]
internal sealed record CachingAspectConfiguration : CachedMethodConfiguration
{
    public bool? UseDependencyInjection { get; init; }

    public CachingAspectConfiguration() { }

#pragma warning disable IDE0051 // Private member is unused
    private CachingAspectConfiguration( CachingAspectConfiguration overrideValue, CachingAspectConfiguration baseValue ) : base(
        overrideValue,
        baseValue )
#pragma warning restore IDE0051 // Private member is unused
    {
        this.UseDependencyInjection = overrideValue.UseDependencyInjection ?? baseValue.UseDependencyInjection;
    }

    public CachingAspectConfiguration ApplyFallbackValues( CachingAspectConfiguration fallback ) => new( this, fallback );
}