// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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