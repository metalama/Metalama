// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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