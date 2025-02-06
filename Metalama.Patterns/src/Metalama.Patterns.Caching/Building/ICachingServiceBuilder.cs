// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters;
using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Formatters;
using Metalama.Patterns.Caching.ValueAdapters;

namespace Metalama.Patterns.Caching.Building;

[PublicAPI]
public interface ICachingServiceBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceProvider"/>.
    /// </summary>
    IServiceProvider? ServiceProvider { get; }

    /// <summary>
    /// Adds a <see cref="CachingProfile"/> to the <see cref="CachingService"/>.
    /// </summary>
    /// <param name="profile">A <see cref="CachingProfile"/>.</param>
    /// <param name="overwrite">A value indicating whether this method is allowed to overwrite an profile with the same name.
    /// When this parameter is <c>false</c>, an exception will be thrown in case of duplicate. The default value is <c>false</c>.</param>
    /// <returns></returns>
    ICachingServiceBuilder AddProfile( CachingProfile profile, bool overwrite = false );

    /// <summary>
    /// Registers an <see cref="IValueAdapter"/> instance and explicitly specifies the value type.
    /// </summary>
    /// <param name="valueType">The type of the cached value (typically the return type of the cached method).</param>
    /// <param name="valueAdapter">The adapter.</param>
    ICachingServiceBuilder AddValueAdapter( Type valueType, IValueAdapter valueAdapter );

    /// <summary>
    /// Registers a generic value adapter.
    /// </summary>
    /// <param name="valueType">The type of the cached value (typically the return type of the cached method).</param>
    /// <param name="valueAdapterType">The type of the value adapter. This type must implement the <see cref="IValueAdapter"/>
    /// interface and have the same number of generic parameters as <paramref name="valueType"/>.
    /// </param>
    ICachingServiceBuilder AddValueAdapter( Type valueType, Type valueAdapterType );

    /// <summary>
    /// Registers an <see cref="IValueAdapter{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the cached value (typically the return type of the cached method).</typeparam>
    /// <param name="valueAdapter">The adapter.</param>
    ICachingServiceBuilder AddValueAdapter<T>( IValueAdapter<T> valueAdapter );

    /// <summary>
    /// Specifies a specific instance of the <see cref="CachingBackend"/> class to be used by the <see cref="CachingService"/>.
    /// </summary>
    /// <param name="backend">A <see cref="CachingBackend"/>.</param>
    /// <param name="ownsBackend">A value indicating whether the <see cref="CachingBackend"/> should be initialized and disposed of
    /// by the <see cref="CachingService"/>. The default value is <c>false</c>.</param>
    /// <returns></returns>
    ICachingServiceBuilder WithBackend( CachingBackend backend, bool ownsBackend = false );

    /// <summary>
    /// Specifies how to create.
    /// </summary>
    /// <param name="action">An action that uses a <see cref="CachingBackendBuilder"/> to specify how to build a <see cref="CachingBackend"/>.</param>
    /// <param name="ownsBackend">A value indicating whether the <see cref="CachingBackend"/> should be initialized and disposed of
    /// by the <see cref="CachingService"/>. The default value is <c>true</c>.</param>
    /// <returns></returns>
    ICachingServiceBuilder WithBackend( Func<CachingBackendBuilder, ConcreteCachingBackendBuilder> action, bool ownsBackend = true );

    /// <summary>
    /// Configures the cache key formatters thanks to a delegate that acts on a <see cref="FormatterRepository.Builder"/>.
    /// </summary>
    ICachingServiceBuilder ConfigureFormatters( Action<FormatterRepository.Builder> action );

    /// <summary>
    /// Replaces the <see cref="CacheKeyBuilder"/> class with your own implementation.
    /// </summary>
    /// <param name="factory">A delegate that creates an object implementing the <see cref="ICacheKeyBuilder"/> interface.</param>
    ICachingServiceBuilder WithKeyBuilder( Func<IFormatterRepository, CacheKeyBuilderOptions, ICacheKeyBuilder> factory );

    /// <summary>
    /// Specifies the <see cref="CacheKeyBuilderOptions"/>.
    /// </summary>
    ICachingServiceBuilder WithKeyBuilderOptions( CacheKeyBuilderOptions options );
}