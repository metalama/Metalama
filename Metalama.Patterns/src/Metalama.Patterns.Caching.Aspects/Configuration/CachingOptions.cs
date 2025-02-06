// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Options;
using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Aspects.Configuration;

#pragma warning disable SA1623

[RunTimeOrCompileTime]
internal sealed record CachingOptions : IHierarchicalOptions<IMethod>, IHierarchicalOptions<INamedType>, IHierarchicalOptions<INamespace>,
                                        IHierarchicalOptions<ICompilation>, ICacheItemConfiguration
{
    // Default compile-time options are all unset (null) because those provided at run-time by the profile must take precedence.
    internal static CachingOptions DefaultCompileTimeOptions { get; } = new();

    // ReSharper disable once UnusedMember.Global
    internal static CachingOptions DefaultProfileOptions { get; } = new()
    {
        UseDependencyInjection = true, Priority = CacheItemPriority.Default, AutoReload = false, IgnoreThisParameter = false
    };

    /// <summary>
    /// Gets or sets the name of the <see cref="CachingProfile"/> that contains the configuration of the cached methods.
    /// </summary>
    public string? ProfileName
    {
        get;
        init;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the method calls are automatically reloaded (by re-evaluating the target method with the same arguments)
    /// when the cache item is removed from the cache.
    /// </summary>
    public bool? AutoReload
    {
        get;
        init;
    }

    /// <summary>
    /// Gets or sets the total duration during which the result of the cached method  is stored in cache. The absolute
    /// expiration time is counted from the moment the method is evaluated and cached.
    /// </summary>
    public TimeSpan? AbsoluteExpiration
    {
        get;
        init;
    }

    /// <summary>
    /// Gets or sets the duration during which the result of the cached method is stored in cache after it has been
    /// added to or accessed from the cache. The expiration is extended every time the value is accessed from the cache.
    /// </summary>
    public TimeSpan? SlidingExpiration
    {
        get;
        init;
    }

    public bool? IsEnabled { get; init; }

    /// <summary>
    /// Gets or sets the priority of the cached method.
    /// </summary>
    public CacheItemPriority? Priority
    {
        get;
        init;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the <c>this</c> instance should be a part of the cache key. The default value of this property is <c>false</c>,
    /// which means that by default the <c>this</c> instance is a part of the cache key.
    /// </summary>
    public bool? IgnoreThisParameter
    {
        get;
        init;
    }

    public bool? UseDependencyInjection { get; init; }

    public IncrementalKeyedCollection<string, ParameterFilterRegistration> ParameterClassifiers { get; init; } =
        IncrementalKeyedCollection<string, ParameterFilterRegistration>.Empty;

    object IIncrementalObject.ApplyChanges( object changes, in ApplyChangesContext context )
    {
        var other = (CachingOptions) changes;

        return new CachingOptions
        {
            AbsoluteExpiration = other.AbsoluteExpiration ?? this.AbsoluteExpiration,
            AutoReload = other.AutoReload ?? this.AutoReload,
            IgnoreThisParameter = other.IgnoreThisParameter ?? this.IgnoreThisParameter,
            Priority = other.Priority ?? this.Priority,
            ProfileName = other.ProfileName ?? this.ProfileName,
            SlidingExpiration = other.SlidingExpiration ?? this.SlidingExpiration,
            UseDependencyInjection = other.UseDependencyInjection ?? this.UseDependencyInjection,
            ParameterClassifiers = this.ParameterClassifiers.AddOrApplyChanges( other.ParameterClassifiers )
        };
    }

    IHierarchicalOptions IHierarchicalOptions.GetDefaultOptions( OptionsInitializationContext context ) => DefaultCompileTimeOptions;
}