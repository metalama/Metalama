// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching;

/// <summary>
/// Configuration of a <see cref="CacheItem"/>.
/// </summary>
[RunTimeOrCompileTime]
[PublicAPI]
public record CacheItemConfiguration : ICacheItemConfiguration
{
    /// <inheritdoc />
    public bool? IsEnabled { get; init; }

    /// <inheritdoc />
    public string? ProfileName { get; init; }

    /// <inheritdoc />
    public bool? AutoReload { get; init; }

    /// <inheritdoc />
    public TimeSpan? AbsoluteExpiration { get; init; }

    /// <inheritdoc />
    public TimeSpan? SlidingExpiration { get; init; }

    /// <inheritdoc />
    public CacheItemPriority? Priority { get; init; }

    public CacheItemConfiguration() { }

    protected CacheItemConfiguration( CacheItemConfiguration overrideValue, ICacheItemConfiguration baseValue )
    {
        this.AutoReload = overrideValue.AutoReload ?? baseValue.AutoReload;
        this.AbsoluteExpiration = overrideValue.AbsoluteExpiration ?? baseValue.AbsoluteExpiration;
        this.SlidingExpiration = overrideValue.SlidingExpiration ?? baseValue.SlidingExpiration;
        this.Priority = overrideValue.Priority ?? baseValue.Priority;
        this.ProfileName = overrideValue.ProfileName ?? baseValue.ProfileName;
        this.IsEnabled = overrideValue.IsEnabled ?? baseValue.IsEnabled;
    }

    public CacheItemConfiguration ApplyBaseValues( ICacheItemConfiguration fallback ) => new( this, fallback );
}