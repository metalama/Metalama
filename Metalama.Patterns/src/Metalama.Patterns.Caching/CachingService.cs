// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace;
using Flashtrace.Formatters;
using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Formatters;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.ValueAdapters;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Patterns.Caching;

/// <summary>
/// Default implementation of the <see cref="ICachingService"/> interface, providing the run-time caching infrastructure.
/// </summary>
/// <remarks>
/// <para>The <see cref="CachingService"/> class is the main run-time component of Metalama Caching. It manages
/// caching profiles, backends, key builders, and value adapters.</para>
/// <para>When using dependency injection, instances are created via <see cref="Building.CachingServiceFactory.AddMetalamaCaching"/>.
/// When not using dependency injection, use the <see cref="Default"/> static property, initialized via the <see cref="Create"/> method.</para>
/// </remarks>
/// <seealso cref="ICachingService"/>
/// <seealso cref="CachingProfile"/>
/// <seealso cref="Building.CachingServiceFactory"/>
/// <seealso href="@caching-getting-started"/>
public sealed partial class CachingService : ICachingService
{
    private readonly FormatterRepository _formatters;
    private readonly bool _ownsBackend;

    /// <summary>
    /// Gets or sets the default <see cref="CachingService"/> instance used when dependency injection is disabled.
    /// </summary>
    /// <remarks>
    /// <para>This property should be initialized during application startup when not using dependency injection.
    /// Use the <see cref="Create"/> method to create an instance.</para>
    /// </remarks>
    public static CachingService Default { get; set; } = CreateUninitialized();

    /// <summary>
    /// Creates a new instance of the <see cref="CachingService"/> class with the specified configuration.
    /// </summary>
    /// <param name="build">An optional delegate to configure the service using <see cref="Building.ICachingServiceBuilder"/>.</param>
    /// <param name="serviceProvider">An optional <see cref="IServiceProvider"/> for resolving dependencies.</param>
    /// <returns>A configured <see cref="CachingService"/> instance.</returns>
    public static CachingService Create( Action<ICachingServiceBuilder>? build = null, IServiceProvider? serviceProvider = null )
    {
        var builder = new Builder( serviceProvider );
        build?.Invoke( builder );
        builder.Dispose();

        var backend = builder.CreateBackend();

        return new CachingService( serviceProvider, backend, builder );
    }

    internal IServiceProvider? ServiceProvider { get; }

    internal AutoReloadManager AutoReloadManager { get; }

    internal CachingFrontend Frontend { get; }

    internal ValueAdapterFactory ValueAdapters { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingService"/> class.
    /// </summary>
    /// <param name="backend">The default back-end. If null, a new <see cref="MemoryCachingBackend"/> is created.</param>
    /// <param name="serviceProvider">An optional <see cref="IServiceProvider"/>.</param>
    private CachingService( IServiceProvider? serviceProvider, CachingBackend? defaultBackend, Builder builder )
    {
        this._ownsBackend = builder.OwnsBackend;

        // Run the builder.
        this.DefaultBackend = defaultBackend ?? CachingBackend.Create( b => b.Memory(), serviceProvider );

        // Set profiles.
        var profilesDictionary = ImmutableDictionary.CreateBuilder<string, CachingProfile>( StringComparer.Ordinal );

        foreach ( var profile in builder.Profiles )
        {
            profilesDictionary.Add( profile.Name, profile );
            profile.Initialize( this.DefaultBackend, serviceProvider );
        }

        if ( !profilesDictionary.ContainsKey( CachingProfile.DefaultName ) )
        {
            var profile = new CachingProfile();
            profilesDictionary.Add( CachingProfile.DefaultName, profile );
            profile.Initialize( this.DefaultBackend, serviceProvider );
        }

        this._formatters = FormatterRepository.Create(
            CacheKeyFormatting.Instance,
            formattersBuilder =>
            {
                formattersBuilder.AddFormatter( typeof(IEnumerable<>), typeof(CollectionFormatter<>) );

                foreach ( var action in builder.FormattersBuildActions )
                {
                    action( formattersBuilder );
                }
            } );

        this.ValueAdapters = new ValueAdapterFactory( builder.ValueAdapters );
        this.ServiceProvider = builder.ServiceProvider;
        this.Profiles = new CachingProfileRegistry( profilesDictionary.ToImmutable() );
        this.AllBackends = [..this.Profiles.AllBackends];
        this.Frontend = new CachingFrontend( this );
        this.AutoReloadManager = new AutoReloadManager( this );
        this.KeyBuilder = builder.CreateKeyBuilder( this._formatters );
        this.Logger = builder.ServiceProvider.GetFlashtraceSource( this.GetType(), FlashtraceRole.Caching );
    }

    internal static CachingService CreateUninitialized( IServiceProvider? serviceProvider = null )
        => Create( b => b.WithBackend( x => x.Uninitialized() ), serviceProvider );

    /// <summary>
    /// Gets the set of distinct backends used in the service.
    /// </summary>
    public ImmutableArray<CachingBackend> AllBackends { get; }

    public async Task InitializeAsync( CancellationToken cancellationToken )
    {
        if ( this._ownsBackend )
        {
            await this.DefaultBackend.InitializeAsync( cancellationToken );
        }

        foreach ( var profile in this.Profiles )
        {
            if ( profile.OwnsBackend )
            {
                await profile.Backend.InitializeAsync( cancellationToken );
            }
        }

        foreach ( var backend in this.AllBackends )
        {
            await backend.InitializeAsync( cancellationToken );
        }
    }

    public FlashtraceSource Logger { get; }

    /// <summary>
    /// Gets the <see cref="CacheKeyBuilder"/> used to generate caching keys, i.e. to serialize objects into a <see cref="string"/>.
    /// </summary>
    [AllowNull]
    public ICacheKeyBuilder KeyBuilder { get; }

    /// <summary>
    /// Gets default <see cref="CachingBackend"/>, i.e. the physical storage of cache items.
    /// </summary>
    [AllowNull]
    public CachingBackend DefaultBackend { get; }

    /// <summary>
    /// Gets the repository of caching profiles (<see cref="CachingProfile"/>).
    /// </summary>
    public CachingProfileRegistry Profiles { get; }

    public void Dispose() => this.Dispose( default );

    public void Dispose( CancellationToken cancellationToken )
    {
        this.AutoReloadManager.Dispose( cancellationToken );

        if ( this._ownsBackend )
        {
            this.DefaultBackend.Dispose( cancellationToken );
        }

        foreach ( var profile in this.Profiles )
        {
            if ( profile.OwnsBackend )
            {
                profile.Backend.Dispose( cancellationToken );
            }
        }
    }

    public ValueTask DisposeAsync() => this.DisposeAsync( default );

    public async ValueTask DisposeAsync( CancellationToken cancellationToken )
    {
        await this.AutoReloadManager.DisposeAsync( cancellationToken );

        if ( this._ownsBackend )
        {
            await this.DefaultBackend.DisposeAsync( cancellationToken );
        }

        foreach ( var profile in this.Profiles )
        {
            if ( profile.OwnsBackend )
            {
                await profile.Backend.DisposeAsync( cancellationToken );
            }
        }
    }
}