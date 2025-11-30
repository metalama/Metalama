// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Options;
using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Aspects.Configuration;

/// <summary>
/// Builder class for configuring caching options through the <see cref="CachingConfigurationExtensions.ConfigureCaching(Metalama.Framework.Fabrics.IQuery{Metalama.Framework.Code.IMethod}, Action{CachingOptionsBuilder})"/>
/// fabric extension method.
/// </summary>
/// <remarks>
/// <para>This class provides the same configuration options as <see cref="CachingConfigurationAttribute"/>,
/// but can be used programmatically in fabrics for more fine-grained control over caching configuration.</para>
/// </remarks>
/// <seealso cref="CachingConfigurationExtensions"/>
/// <seealso cref="CachingConfigurationAttribute"/>
/// <seealso cref="ICacheParameterClassifier"/>
/// <seealso href="@caching-configuration"/>
[PublicAPI]
[CompileTime]
public sealed class CachingOptionsBuilder
{
    private IncrementalKeyedCollection<string, ParameterFilterRegistration> _parameterClassifiers =
        IncrementalKeyedCollection<string, ParameterFilterRegistration>.Empty;

    /// <summary>
    /// Gets or sets the name of the <see cref="CachingProfile"/> that contains the configuration of the cached methods.
    /// </summary>
    public string? ProfileName
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the method calls are automatically reloaded (by re-evaluating the target method with the same arguments)
    /// when the cache item is removed from the cache.
    /// </summary>
    public bool? AutoReload
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the total duration during which the result of the cached method  is stored in cache. The absolute
    /// expiration time is counted from the moment the method is evaluated and cached.
    /// </summary>
    public TimeSpan? AbsoluteExpiration
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the duration during which the result of the cached method is stored in cache after it has been
    /// added to or accessed from the cache. The expiration is extended every time the value is accessed from the cache.
    /// </summary>
    public TimeSpan? SlidingExpiration
    {
        get;
        set;
    }

    public bool? IsEnabled { get; init; }

    /// <summary>
    /// Gets or sets the priority of the cached method.
    /// </summary>
    public CacheItemPriority? Priority
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the <c>this</c> instance should be a part of the cache key. The default value of this property is <c>false</c>,
    /// which means that by default the <c>this</c> instance is a part of the cache key.
    /// </summary>
    public bool? IgnoreThisParameter
    {
        get;
        set;
    }

    public bool? UseDependencyInjection { get; set; }

    /// <summary>
    /// Registers a parameter classifier that determines how method parameters are handled in cache key generation.
    /// </summary>
    /// <param name="name">A unique name for this classifier registration, used to identify and potentially remove it later.</param>
    /// <param name="classifier">The classifier implementation that evaluates parameters and returns a <see cref="CacheParameterClassification"/>.</param>
    /// <seealso cref="ICacheParameterClassifier"/>
    /// <seealso cref="CacheParameterClassification"/>
    public void AddParameterClassifier( string name, ICacheParameterClassifier classifier )
    {
        this._parameterClassifiers = this._parameterClassifiers.AddOrApplyChanges( new ParameterFilterRegistration( name, classifier ) );
    }

    /// <summary>
    /// Removes a previously registered parameter classifier by name.
    /// </summary>
    /// <param name="name">The name of the classifier registration to remove.</param>
    public void RemoveParameterClassifier( string name )
    {
        this._parameterClassifiers = this._parameterClassifiers.Remove( name );
    }

    internal CachingOptions Build()
        => new()
        {
            AbsoluteExpiration = this.AbsoluteExpiration,
            AutoReload = this.AutoReload,
            IgnoreThisParameter = this.IgnoreThisParameter,
            Priority = this.Priority,
            ProfileName = this.ProfileName,
            SlidingExpiration = this.SlidingExpiration,
            UseDependencyInjection = this.UseDependencyInjection,
            IsEnabled = this.IsEnabled,
            ParameterClassifiers = this._parameterClassifiers
        };
}