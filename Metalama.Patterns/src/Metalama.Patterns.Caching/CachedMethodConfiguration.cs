// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Caching;

[PublicAPI]
[RunTimeOrCompileTime]
public record CachedMethodConfiguration : CacheItemConfiguration
{
    public CachedMethodConfiguration() { }

    public static CachedMethodConfiguration Empty { get; } = new();

    public bool? IgnoreThisParameter { get; init; }

    protected CachedMethodConfiguration( CachedMethodConfiguration overrideValue, CachedMethodConfiguration baseValue ) : base(
        overrideValue,
        baseValue )
    {
        this.IgnoreThisParameter = overrideValue.IgnoreThisParameter ?? baseValue.IgnoreThisParameter;
    }
}