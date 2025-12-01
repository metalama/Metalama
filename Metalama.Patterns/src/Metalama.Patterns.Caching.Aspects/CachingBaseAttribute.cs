// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Options;
using Metalama.Patterns.Caching.Aspects.Configuration;
using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Aspects;

/// <summary>
/// Abstract base class for <see cref="CacheAttribute"/> and <see cref="CachingConfigurationAttribute"/>,
/// providing common caching configuration properties.
/// </summary>
/// <remarks>
/// <para>This class provides compile-time configuration properties that control caching behavior such as
/// expiration times, priority, and cache key generation. Properties set on this attribute take precedence
/// over run-time configuration specified through <see cref="CachingProfile"/>.</para>
/// </remarks>
/// <seealso cref="CacheAttribute"/>
/// <seealso cref="CachingConfigurationAttribute"/>
/// <seealso cref="CachingProfile"/>
/// <seealso href="@caching-configuration"/>
[RunTimeOrCompileTime]
public abstract class CachingBaseAttribute : Attribute, IHierarchicalOptionsProvider
{
    private CachingOptions _options = new();

    /// <summary>
    /// Gets or sets the name of the <see cref="CachingProfile"/> that contains the configuration of the cached methods.
    /// </summary>
    public string? ProfileName
    {
        get => this._options.ProfileName ?? CachingOptions.DefaultCompileTimeOptions.ProfileName;
        set => this._options = this._options with { ProfileName = value };
    }

    /// <summary>
    /// Gets or sets a value indicating whether the method calls are automatically reloaded (by re-evaluating the target method with the same arguments)
    /// when the cache item is removed from the cache.
    /// </summary>
    public bool AutoReload
    {
        get => this._options.AutoReload ?? CachingOptions.DefaultCompileTimeOptions.AutoReload!.Value;
        set => this._options = this._options with { AutoReload = value };
    }

    /// <summary>
    /// Gets or sets the total duration, in minutes, during which the result of the cached method  is stored in cache. The absolute
    /// expiration time is counted from the moment the method is evaluated and cached.
    /// </summary>
    public double AbsoluteExpiration
    {
        get => (this._options.AbsoluteExpiration ?? CachingOptions.DefaultCompileTimeOptions.AbsoluteExpiration)?.TotalMinutes ?? 0;
        set => this._options = this._options with { AbsoluteExpiration = TimeSpan.FromMinutes( value ) };
    }

    /// <summary>
    /// Gets or sets the duration, in minutes, during which the result of the cached method is stored in cache after it has been
    /// added to or accessed from the cache. The expiration is extended every time the value is accessed from the cache.
    /// </summary>
    public double SlidingExpiration
    {
        get => (this._options.SlidingExpiration ?? CachingOptions.DefaultCompileTimeOptions.SlidingExpiration)?.TotalMinutes ?? 0;
        set => this._options = this._options with { SlidingExpiration = TimeSpan.FromMinutes( value ) };
    }

    /// <summary>
    /// Gets or sets the priority of the cached method.
    /// </summary>
    public CacheItemPriority Priority
    {
        get => this._options.Priority ?? CachingOptions.DefaultCompileTimeOptions.Priority!.Value;
        set => this._options = this._options with { Priority = value };
    }

    /// <summary>
    /// Gets or sets a value indicating whether the <c>this</c> instance should be a part of the cache key. The default value of this property is <c>false</c>,
    /// which means that by default the <c>this</c> instance is a part of the cache key.
    /// </summary>
    public bool IgnoreThisParameter
    {
        get => this._options.IgnoreThisParameter ?? CachingOptions.DefaultCompileTimeOptions.IgnoreThisParameter!.Value;
        set => this._options = this._options with { IgnoreThisParameter = value };
    }

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="ICachingService"/> should be obtained through dependency injection.
    /// The default value is <c>true</c>. When set to <c>false</c>, the <see cref="CachingService.Default"/> static property is used instead.
    /// </summary>
    /// <remarks>
    /// <para>When dependency injection is enabled, the aspect introduces a dependency to <see cref="ICachingService"/> into
    /// the declaring type, which requires the type to be instantiated through a dependency injection container.</para>
    /// <para>When dependency injection is disabled, static methods can also be cached.</para>
    /// </remarks>
    public bool UseDependencyInjection
    {
        get => this._options.UseDependencyInjection ?? CachingOptions.DefaultCompileTimeOptions.UseDependencyInjection!.Value;
        set => this._options = this._options with { UseDependencyInjection = value };
    }

    public IEnumerable<IHierarchicalOptions> GetOptions( in OptionsProviderContext context ) => new[] { this._options };
}