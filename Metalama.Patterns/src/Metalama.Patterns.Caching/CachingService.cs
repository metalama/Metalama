// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

public sealed partial class CachingService : ICachingService
{
    private readonly FormatterRepository _formatters;
    private readonly bool _ownsBackend;

    public static CachingService Default { get; set; } = CreateUninitialized();

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