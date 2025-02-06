// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Building;

namespace Metalama.Patterns.Caching.Backends.Azure;

/// <summary>
/// Extension methods for <see cref="MemoryCachingBackendBuilder"/>.
/// </summary>
[PublicAPI]
public static class AzureCachingFactory
{
    /// <summary>
    /// Adds a component that synchronizes several local, in-memory caches, using Azure Service Bus. This overloads expects a
    /// connection string to the Azure Service Bus topic.
    /// </summary>
    public static AzureCacheSynchronizerBuilder WithAzureSynchronization(
        this MemoryCachingBackendBuilder builder,
        string connectionString )
        => builder.WithAzureSynchronization( new AzureCacheSynchronizerConfiguration( connectionString ) );

    /// <summary>
    /// Enhances an in-memory cache with a component that synchronizes several local, in-memory caches, using Azure Service Bus. This overloads expects a
    /// an <see cref="AzureCacheSynchronizerConfiguration"/>.
    /// </summary>
    public static AzureCacheSynchronizerBuilder WithAzureSynchronization(
        this MemoryCachingBackendBuilder builder,
        AzureCacheSynchronizerConfiguration configuration )
        => new( builder, configuration, builder.ServiceProvider );
}