// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters;
using Flashtrace.Formatters.TypeExtensions;
using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Formatters;
using Metalama.Patterns.Caching.Utilities;
using Metalama.Patterns.Caching.ValueAdapters;

namespace Metalama.Patterns.Caching;

public partial class CachingService
{
    [PublicAPI]
    internal sealed class Builder : ICachingServiceBuilder, IDisposable
    {
        private readonly Dictionary<string, CachingProfile> _profiles = new();
        private readonly List<Action<FormatterRepository.Builder>> _formattersBuildActions = new();
        private bool _isDisposed;
        private CachingBackend? _specificBackend;
        private Func<CachingBackendBuilder, ConcreteCachingBackendBuilder>? _cachingBackendBuildAction;

        internal TypeExtensionFactory<IValueAdapter> ValueAdapters { get; } = new( typeof(IValueAdapter<>), null, null );

        internal Builder( IServiceProvider? serviceProvider )
        {
            this.ServiceProvider = serviceProvider ?? NullServiceProvider.Instance;

            this.AddValueAdapter( new StreamAdapter() );
            this.AddValueAdapter( typeof(IEnumerable<>), typeof(EnumerableAdapter<>) );
            this.AddValueAdapter( typeof(IEnumerator<>), typeof(EnumeratorAdapter<>) );
#if NETCOREAPP3_0_OR_GREATER
            this.AddValueAdapter( typeof(IAsyncEnumerable<>), typeof(AsyncEnumerableAdapter<>) );
            this.AddValueAdapter( typeof(IAsyncEnumerator<>), typeof(AsyncEnumeratorAdapter<>) );
#endif
        }

        private void CheckNotDisposed()
        {
            if ( this._isDisposed )
            {
                throw new ObjectDisposedException( this.GetType().FullName );
            }
        }

        public IServiceProvider ServiceProvider { get; }

        public bool OwnsBackend { get; set; }

        public CachingBackend? CreateBackend()
        {
            var backend = this._specificBackend;

            if ( backend != null )
            {
                return backend;
            }
            else if ( this._cachingBackendBuildAction != null )
            {
                return CachingBackend.Create( this._cachingBackendBuildAction, this.ServiceProvider );
            }
            else
            {
                return null;
            }
        }

        internal IReadOnlyCollection<CachingProfile> Profiles => this._profiles.Values;

        internal IReadOnlyList<Action<FormatterRepository.Builder>> FormattersBuildActions => this._formattersBuildActions;

        public Func<IFormatterRepository, CacheKeyBuilderOptions, ICacheKeyBuilder> KeyBuilderFactory { get; set; }
            = ( formatters, options ) => new CacheKeyBuilder( formatters, options );

        public CacheKeyBuilderOptions KeyBuilderOptions { get; set; } = new();

        public ICacheKeyBuilder CreateKeyBuilder( IFormatterRepository formatters ) => this.KeyBuilderFactory( formatters, this.KeyBuilderOptions );

        public ICachingServiceBuilder AddProfile( CachingProfile profile, bool skipIfExists = false )
        {
            this.CheckNotDisposed();

            if ( !skipIfExists || !this._profiles.ContainsKey( profile.Name ) )
            {
                this._profiles.Add( profile.Name, profile );
            }

            return this;
        }

        /// <summary>
        /// Registers an <see cref="IValueAdapter"/> instance and explicitly specifies the value type.
        /// </summary>
        /// <param name="valueType">The type of the cached value (typically the return type of the cached method).</param>
        /// <param name="valueAdapter">The adapter.</param>
        public ICachingServiceBuilder AddValueAdapter( Type valueType, IValueAdapter valueAdapter )
        {
            this.CheckNotDisposed();
            this.ValueAdapters.RegisterTypeExtension( valueType, valueAdapter );

            return this;
        }

        /// <summary>
        /// Registers a generic value adapter.
        /// </summary>
        /// <param name="valueType">The type of the cached value (typically the return type of the cached method).</param>
        /// <param name="valueAdapterType">The type of the value adapter. This type must implement the <see cref="IValueAdapter"/>
        /// interface and have the same number of generic parameters as <paramref name="valueType"/>.
        /// </param>
        public ICachingServiceBuilder AddValueAdapter( Type valueType, Type valueAdapterType )
        {
            this.CheckNotDisposed();
            this.ValueAdapters.RegisterTypeExtension( valueType, valueAdapterType );

            return this;
        }

        /// <summary>
        /// Registers an <see cref="IValueAdapter{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the cached value (typically the return type of the cached method).</typeparam>
        /// <param name="valueAdapter">The adapter.</param>
        public ICachingServiceBuilder AddValueAdapter<T>( IValueAdapter<T> valueAdapter )
        {
            this.CheckNotDisposed();
            this.AddValueAdapter( typeof(T), valueAdapter );

            return this;
        }

        public ICachingServiceBuilder WithBackend( CachingBackend backend, bool ownsBackend )
        {
            this._specificBackend = backend;
            this.OwnsBackend = ownsBackend;

            return this;
        }

        public ICachingServiceBuilder WithBackend( Func<CachingBackendBuilder, ConcreteCachingBackendBuilder> action, bool ownsBackend )
        {
            this._cachingBackendBuildAction = action;
            this.OwnsBackend = ownsBackend;

            return this;
        }

        public ICachingServiceBuilder ConfigureFormatters( Action<FormatterRepository.Builder> action )
        {
            this.CheckNotDisposed();
            this._formattersBuildActions.Add( action );

            return this;
        }

        public ICachingServiceBuilder WithKeyBuilder( Func<IFormatterRepository, CacheKeyBuilderOptions, ICacheKeyBuilder> factory )
        {
            this.CheckNotDisposed();

            this.KeyBuilderFactory = factory;

            return this;
        }

        public ICachingServiceBuilder WithKeyBuilderOptions( CacheKeyBuilderOptions options )
        {
            this.CheckNotDisposed();

            this.KeyBuilderOptions = options;

            return this;
        }

        public void Dispose()
        {
            this._isDisposed = true;
        }
    }
}