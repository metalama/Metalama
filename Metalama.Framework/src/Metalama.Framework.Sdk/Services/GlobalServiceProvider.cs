// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Services;
using System;

namespace Metalama.Framework.Engine.Services;

/// <summary>
/// Provides access to globally scoped services. A wrapper around <see cref="ServiceProvider{T}"/> for <see cref="IGlobalService"/>.
/// </summary>
/// <remarks>
/// <para>Global services are singleton services that are shared across the entire Metalama process. Use <see cref="GetService{T}"/>
/// or <see cref="GetRequiredService{T}"/> to retrieve services from this provider.</para>
/// <para>This type is immutable. Methods like <see cref="WithService"/> and <see cref="WithServices"/> return a new instance
/// instead of modifying the current one.</para>
/// </remarks>
/// <seealso cref="ProjectServiceProvider"/>
/// <seealso cref="ServiceProvider{T}"/>
/// <seealso cref="IGlobalService"/>
[PublicAPI]
public readonly struct GlobalServiceProvider
{
    /// <summary>
    /// Gets the underlying <see cref="ServiceProvider{T}"/> for <see cref="IGlobalService"/>.
    /// </summary>
    public ServiceProvider<IGlobalService> Underlying { get; }

    /// <summary>
    /// Gets a value indicating whether the current <see cref="GlobalServiceProvider"/> is null.
    /// </summary>
    public bool IsNull => this.Underlying == null;

    private GlobalServiceProvider( ServiceProvider<IGlobalService> serviceProvider )
    {
        this.Underlying = serviceProvider;
    }

    /// <summary>
    /// Gets a required service from the provider.
    /// </summary>
    /// <typeparam name="T">The type of the service to retrieve.</typeparam>
    /// <returns>The service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not found.</exception>
    public T GetRequiredService<T>()
        where T : class, IGlobalService
        => this.Underlying.GetService<T>() ?? throw new InvalidOperationException( $"Cannot get the service {typeof(T).Name}." );

    /// <summary>
    /// Gets an optional service from the provider.
    /// </summary>
    /// <typeparam name="T">The type of the service to retrieve.</typeparam>
    /// <returns>The service instance, or <c>null</c> if the service is not found.</returns>
    public T? GetService<T>()
        where T : class, IGlobalService
        => this.Underlying.GetService<T>();

    /// <summary>
    /// Implicitly converts a <see cref="ServiceProvider{T}"/> to a <see cref="GlobalServiceProvider"/>.
    /// </summary>
    public static implicit operator GlobalServiceProvider( ServiceProvider<IGlobalService> serviceProvider ) => new( serviceProvider );

    /// <summary>
    /// Implicitly converts a <see cref="GlobalServiceProvider"/> to a <see cref="ServiceProvider{T}"/>.
    /// </summary>
    public static implicit operator ServiceProvider<IGlobalService>( GlobalServiceProvider serviceProvider ) => serviceProvider.Underlying;

    /// <summary>
    /// Returns a new <see cref="GlobalServiceProvider"/> with an additional service.
    /// </summary>
    /// <param name="service">The service to add.</param>
    /// <returns>A new provider instance with the added service.</returns>
    public GlobalServiceProvider WithService( IGlobalService service ) => this.Underlying.WithService( service );

    /// <summary>
    /// Returns a new <see cref="GlobalServiceProvider"/> with additional services.
    /// </summary>
    /// <param name="services">The services to add.</param>
    /// <returns>A new provider instance with the added services.</returns>
    public GlobalServiceProvider WithServices( params IGlobalService[] services ) => this.Underlying.WithServices( services );

    /// <inheritdoc />
    public override string ToString() => this.Underlying.ToString();
}