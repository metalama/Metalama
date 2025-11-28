// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Services;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Services;

/// <summary>
/// Provides access to project-scoped services. A wrapper around <see cref="ServiceProvider{T}"/> for <see cref="IProjectService"/>.
/// </summary>
/// <remarks>
/// <para>Project services are scoped to a specific compilation or project. This provider also provides access to global services
/// through the <see cref="Global"/> property.</para>
/// <para>This type is immutable. Methods like <see cref="WithService"/> and <see cref="WithServices(IEnumerable{IProjectService})"/> return a new instance
/// instead of modifying the current one.</para>
/// </remarks>
/// <seealso cref="GlobalServiceProvider"/>
/// <seealso cref="ServiceProvider{T}"/>
/// <seealso cref="IProjectService"/>
[PublicAPI]
public readonly struct ProjectServiceProvider
{
    /// <summary>
    /// Gets the underlying <see cref="ServiceProvider{T}"/> for <see cref="IProjectService"/>.
    /// </summary>
    public ServiceProvider<IProjectService> Underlying { get; }

    private readonly ServiceProvider<IGlobalService>? _global;

    /// <summary>
    /// Gets the <see cref="GlobalServiceProvider"/> that provides access to global services.
    /// </summary>
    public GlobalServiceProvider Global => this._global ?? throw new InvalidOperationException();

    /// <summary>
    /// Gets an empty <see cref="ProjectServiceProvider"/> instance.
    /// </summary>
    public static ProjectServiceProvider Empty => new( ServiceProvider<IProjectService>.Empty, ServiceProvider<IGlobalService>.Empty );

    private ProjectServiceProvider( ServiceProvider<IProjectService> underlying, ServiceProvider<IGlobalService> global )
    {
        this.Underlying = underlying;

        // We cache the global service provider because it is used often.

        this._global = global;
    }

    private ProjectServiceProvider( ServiceProvider<IProjectService> serviceProvider ) : this( serviceProvider, serviceProvider.FindNext<IGlobalService>() ) { }

    /// <summary>
    /// Gets a required service from the provider.
    /// </summary>
    /// <typeparam name="T">The type of the service to retrieve.</typeparam>
    /// <returns>The service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not found.</exception>
    public T GetRequiredService<T>()
        where T : class, IProjectService
        => this.Underlying.GetService<T>() ?? throw new InvalidOperationException( $"Cannot get the service {typeof(T)}." );

    /// <summary>
    /// Gets an optional service from the provider.
    /// </summary>
    /// <typeparam name="T">The type of the service to retrieve.</typeparam>
    /// <returns>The service instance, or <c>null</c> if the service is not found.</returns>
    public T? GetService<T>()
        where T : class, IProjectService
        => this.Underlying.GetService<T>();

    /// <summary>
    /// Implicitly converts a <see cref="ServiceProvider{T}"/> to a <see cref="ProjectServiceProvider"/>.
    /// </summary>
    public static implicit operator ProjectServiceProvider( ServiceProvider<IProjectService> serviceProvider ) => new( serviceProvider );

    /// <summary>
    /// Implicitly converts a <see cref="ProjectServiceProvider"/> to a <see cref="ServiceProvider{T}"/>.
    /// </summary>
    public static implicit operator ServiceProvider<IProjectService>( in ProjectServiceProvider serviceProvider ) => serviceProvider.Underlying;

    /// <summary>
    /// Implicitly converts a <see cref="ProjectServiceProvider"/> to a <see cref="GlobalServiceProvider"/>.
    /// </summary>
    public static implicit operator GlobalServiceProvider( in ProjectServiceProvider serviceProvider ) => serviceProvider.Global;

    /// <summary>
    /// Returns a new <see cref="ProjectServiceProvider"/> with an additional service.
    /// </summary>
    /// <param name="service">The service to add.</param>
    /// <param name="allowOverride">Indicates whether to allow overriding an existing service.</param>
    /// <returns>A new provider instance with the added service.</returns>
    public ProjectServiceProvider WithService( IProjectService service, bool allowOverride = false ) => this.Underlying.WithService( service, allowOverride );

    /// <summary>
    /// Returns a new <see cref="ServiceProvider{T}"/> with additional services.
    /// </summary>
    /// <param name="services">The services to add.</param>
    /// <returns>A new provider instance with the added services.</returns>
    public ServiceProvider<IProjectService> WithServices( params IProjectService[] services ) => this.Underlying.WithServices( services );

    /// <summary>
    /// Returns a new <see cref="ServiceProvider{T}"/> with additional services.
    /// </summary>
    /// <param name="services">The services to add.</param>
    /// <returns>A new provider instance with the added services.</returns>
    public ServiceProvider<IProjectService> WithServices( IEnumerable<IProjectService> services ) => this.Underlying.WithServices( services );

    /// <inheritdoc />
    public override string ToString() => this.Underlying.ToString();
}