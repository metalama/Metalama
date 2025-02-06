// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Building;

namespace Metalama.Patterns.Caching.Backends.Redis;

/// <summary>
/// Builds a <see cref="CachingBackend"/> that relies on a Redis server.
/// </summary>
[PublicAPI]
public sealed class RedisCachingBackendBuilder : OutOfProcessCachingBackendBuilder
{
    private const int _serializationVersion = 2; // Increase when the serialization protocol changes.

    private readonly RedisCachingBackendConfiguration _configuration;

    internal RedisCachingBackendBuilder(
        RedisCachingBackendConfiguration? configuration,
        IServiceProvider? serviceProvider ) : base( serviceProvider )
    {
        this._configuration = configuration ?? new RedisCachingBackendConfiguration();
    }

    public override CachingBackend CreateBackend( CreateBackendArgs args )
    {
        var configuration = this._configuration;

        // Compute the final key prefix.
        var prefixSuffix = $"V{_serializationVersion}";

        if ( args.Layer != 1 )
        {
            // #20775 Caching: two-layered cache should modify the key to avoid conflicts when toggling the option
            prefixSuffix = $".L{args.Layer}";
        }

        configuration = configuration with
        {
            KeyPrefix = this._configuration.KeyPrefix != null ? this._configuration.KeyPrefix + "." + prefixSuffix : prefixSuffix
        };

        // Instantiate the caching back-end.
        if ( configuration.SupportsDependencies )
        {
            var backend = new DependenciesRedisCachingBackend( configuration, this.ServiceProvider );

            if ( configuration.RunGarbageCollector )
            {
                // This conveniently binds the lifetime of the collector with the one of the back-end.
                backend.Collector = new RedisCacheDependencyGarbageCollector( backend );
            }

            return backend;
        }
        else
        {
            return new RedisCachingBackend( configuration, this.ServiceProvider );
        }
    }
}