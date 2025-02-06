// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Options;
using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Aspects.Configuration;

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

    public void AddParameterClassifier( string name, ICacheParameterClassifier classifier )
    {
        this._parameterClassifiers = this._parameterClassifiers.AddOrApplyChanges( new ParameterFilterRegistration( name, classifier ) );
    }

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