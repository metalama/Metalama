// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.Locking;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Patterns.Caching;

#pragma warning disable SA1623

/// <summary>
/// Set of options defined at run time when the <see cref="CachingService"/> is instantiated. Classes and methods can be assigned
/// to a <see cref="CachingProfile"/> using the <c>ProfileName</c> option or attribute property. Any compile-time setting takes
/// precedence over the values defined in the <see cref="CachingProfile"/>. 
/// </summary>
[PublicAPI]
[RunTime]
public sealed class CachingProfile : ICacheItemConfiguration
{
    /// <summary>
    /// The name of the default profile.
    /// </summary>
    public const string DefaultName = "default";

    private readonly ConcurrentDictionary<int, ICacheItemConfiguration> _mergedMethodConfigurations = new();
    private readonly bool? _ownsBackend;

    private bool _attached;
    private CachingBackend? _backend;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingProfile"/> class.
    /// </summary>
    /// <param name="name">Profile name (a case-insensitive string).</param>
    /// <param name="cachingService"></param>
    public CachingProfile( string name = DefaultName )
    {
        this.Name = name;
    }

    /// <summary>
    /// Gets the profile name  (a case-insensitive string).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the <see cref="CachingBackend"/> active for the current <see cref="CachingProfile"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The <see cref="CachingProfile"/> has not been added to a <see cref="CachingService"/> yet.</exception>
    [AllowNull]
    public CachingBackend Backend => this._backend ?? throw new InvalidOperationException();

    internal void Initialize( CachingBackend defaultBackend, IServiceProvider? serviceProvider )
    {
        if ( this._attached )
        {
            throw new InvalidOperationException( $"A {nameof(CachingProfile)} cannot be used in more than one {nameof(CachingService)}." );
        }

        this._attached = true;

        if ( this.BackendFactory != null )
        {
            this._backend = CachingBackend.Create( this.BackendFactory, serviceProvider );
        }
        else
        {
            this._backend ??= defaultBackend;
        }
    }

    /// <summary>
    /// Gets or sets a delegate creating the <see cref="CachingBackend"/>.
    /// </summary>
    public Func<CachingBackendBuilder, ConcreteCachingBackendBuilder>? BackendFactory { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="CachingService"/> owns the <see cref="Backend"/>. The default value of this property
    /// is <c>true</c> if <see cref="BackendFactory"/> is non-null and <c>false</c> otherwise. When the profile owns the <see cref="CachingBackend"/>,
    /// When the backend is owned, the <see cref="CachingService"/> will initialize it and dispose it. Otherwise, this should be performed by the caller.
    /// </summary>
    public bool OwnsBackend
    {
        get => this._ownsBackend ?? this.BackendFactory != null;
        init => this._ownsBackend = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether caching is enabled for the current profile.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the method calls are automatically reloaded (by re-evaluating the target method with the same arguments)
    /// when the cache item is removed from the cache.
    /// </summary>
    public bool AutoReload { get; init; }

    /// <summary>
    /// Gets or sets the total duration during which the result of the current method is stored in cache. The absolute
    /// expiration time is counted from the moment the method is evaluated and cached.
    /// </summary>
    public TimeSpan? AbsoluteExpiration { get; init; }

    /// <summary>
    /// Gets or sets the duration during which the result of the current method is stored in cache after it has been
    /// added to or accessed from the cache. The expiration is extended every time the value is accessed from the cache.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; init; }

    /// <summary>
    /// Gets or sets the priority of the cached methods.
    /// </summary>
    public CacheItemPriority? Priority { get; init; }

    /// <inheritdoc />
    bool? ICacheItemConfiguration.IsEnabled => this.IsEnabled;

    /// <inheritdoc />
    bool? ICacheItemConfiguration.AutoReload => this.AutoReload;

    /// <inheritdoc />
    string ICacheItemConfiguration.ProfileName => this.Name;

    /// <summary>
    /// Gets or sets the lock manager used to synchronize the execution of methods
    /// that are cached with the current profile.  The default lock manager is <see cref="NullLockingStrategy"/>,
    /// which allows for concurrent execution of all methods. An alternative implementation is <see cref="LocalLockingStrategy"/>,
    /// which prevents concurrent execution of the same method with the same parameters, in the current process (or AppDomain).
    /// </summary>
    public ILockingStrategy LockingStrategy { get; init; } = new NullLockingStrategy();

    /// <summary>
    /// Gets or sets the maximum time that the caching aspect will wait for the <see cref="LockingStrategy"/> to acquire a lock.
    /// To specify an infinite waiting time, set this property to <c>TimeSpan.FromMilliseconds( -1 )</c>. The default
    /// behavior is to wait infinitely.
    /// </summary>
    public TimeSpan AcquireLockTimeout { get; init; } = TimeSpan.FromMilliseconds( -1 );

    /// <summary>
    /// Gets or sets the behavior in case that the caching aspect could not acquire a lock because of a timeout. 
    /// The default behavior is to throw a <see cref="TimeoutException"/>.
    /// If the delegate does not return any exception, the cached method will be evaluated (even without a lock).
    /// </summary>
    public Action<LockTimeoutContext> OnLockTimeout { get; init; } =
        _ => throw new TimeoutException( "Time out while waiting for the cache lock." );

    internal ICacheItemConfiguration GetMergedConfiguration( CachedMethodMetadata metadata )
    {
        if ( this._mergedMethodConfigurations.TryGetValue( metadata.Id, out var configuration ) )
        {
            return configuration;
        }
        else
        {
            // ReSharper disable once HeapView.CanAvoidClosure
            return this._mergedMethodConfigurations.GetOrAdd(
                metadata.Id,
                _ => metadata.Configuration.ApplyBaseValues( this ) );
        }
    }
}