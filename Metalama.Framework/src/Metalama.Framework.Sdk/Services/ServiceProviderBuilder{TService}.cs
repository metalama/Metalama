// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Services;

// ReSharper disable ClassCanBeSealed.Global
/// <summary>
/// A mutable builder for creating <see cref="ServiceProvider{T}"/> instances. Allows registering service factories
/// that will be applied when building the final immutable service provider.
/// </summary>
/// <typeparam name="TService">The base interface type for services in this builder.</typeparam>
/// <remarks>
/// Use this builder to register services before building a <see cref="ServiceProvider{TService}"/>.
/// Services can be registered either eagerly (with an instance) or lazily (with a factory function).
/// </remarks>
[PublicAPI]
[CompileTime]
public class ServiceProviderBuilder<TService>
    where TService : class
{
    private readonly List<Func<ServiceProvider<TService>, ServiceProvider<TService>>> _buildActions = new();

    public ServiceProviderBuilder() { }

    public ServiceProviderBuilder( ServiceProviderBuilder<TService> prototype )
    {
        this._buildActions.AddRange( prototype._buildActions );
    }

    public ServiceProviderBuilder( params TService[] services )
    {
        foreach ( var service in services )
        {
            this.Add( service );
        }
    }

    /// <summary>
    /// Adds a lazily-created service to the builder.
    /// </summary>
    /// <typeparam name="T">The interface type under which the service will be registered. This should be the
    /// service interface (e.g., <c>IMetricProvider&lt;SyntaxNodesCount&gt;</c>), not the concrete implementation type.</typeparam>
    /// <param name="func">A factory function that creates the service instance when needed.</param>
    /// <param name="allowOverride">If <c>true</c>, allows overriding an existing service of the same type.</param>
    public void Add<T>( Func<ServiceProvider<TService>, T> func, bool allowOverride = false )
        where T : class, TService
    {
        this._buildActions.Add( serviceProvider => serviceProvider.WithService( func, allowOverride ) );
    }

    /// <summary>
    /// Adds a lazily-created shared service to the builder. Shared services are instantiated once
    /// and shared across all service provider instances in the same family.
    /// </summary>
    /// <typeparam name="T">The interface type under which the service will be registered. This should be the
    /// service interface (e.g., <c>IMetricProvider&lt;SyntaxNodesCount&gt;</c>), not the concrete implementation type.</typeparam>
    /// <param name="func">A factory function that creates the service instance when needed.</param>
    [PublicAPI]
    public void AddShared<T>( Func<ServiceProvider<TService>, T> func )
        where T : class, TService
    {
        this._buildActions.Add( serviceProvider => serviceProvider.AddSharedService( func ) );
    }

    /// <summary>
    /// Adds a service instance to the builder.
    /// </summary>
    /// <param name="service">The service instance to add.</param>
    /// <param name="allowOverride">If <c>true</c>, allows overriding an existing service of the same type.</param>
    [PublicAPI]
    public void Add( TService service, bool allowOverride = false )
    {
        this._buildActions.Add( serviceProvider => serviceProvider.WithService( service, allowOverride ) );
    }

    /// <summary>
    /// Adds a shared service instance to the builder. Shared services are shared across all
    /// service provider instances in the same family.
    /// </summary>
    /// <param name="service">The service instance to add.</param>
    [PublicAPI]
    public void AddShared( TService service )
    {
        this._buildActions.Add( serviceProvider => serviceProvider.AddSharedService( service ) );
    }

    internal void AddUntyped( Type interfaceType, object implementationInstance, bool allowOverride = false )
    {
        this._buildActions.Add( serviceProvider => serviceProvider.WithUntypedService( interfaceType, implementationInstance, allowOverride ) );
    }

    /// <summary>
    /// Adds a transformation function that modifies the service provider during the build process.
    /// </summary>
    /// <param name="func">A function that takes a service provider and returns a modified service provider.</param>
    public void Add( Func<ServiceProvider<TService>, ServiceProvider<TService>> func ) => this._buildActions.Add( func );

    /// <summary>
    /// Builds a <see cref="ServiceProvider{TService}"/> by applying all registered build actions to an initial provider.
    /// </summary>
    /// <param name="initial">The initial service provider to build upon.</param>
    /// <returns>A new service provider with all registered services.</returns>
    public ServiceProvider<TService> Build( ServiceProvider<TService> initial )
    {
        var current = initial;

        foreach ( var action in this._buildActions )
        {
            current = action( current );
        }

        return current;
    }
}
