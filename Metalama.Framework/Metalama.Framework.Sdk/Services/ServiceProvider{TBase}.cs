// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.Services;

/// <summary>
/// An immutable implementation of <see cref="IServiceProvider"/> that will index services that implement the <typeparamref name="TBase"/> interface.
/// When a service is added to a <see cref="ServiceProvider{TBase}"/>, a mapping is created between the type of this object and the object itself,
/// but also between the type of any interface derived from <typeparamref name="TBase"/> and implemented by this object. Service provider instances
/// are immutable (each <see cref="WithService"/> method
/// returns a copy of the service provider with the new service), except for the <i>shared</i> services, which are shared among all instances
/// of the same family. 
/// </summary>
[PublicAPI]
public sealed partial class ServiceProvider<TBase> : ServiceProvider, IServiceProvider<TBase>
    where TBase : class
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly ConcurrentDictionary<Type, ImmutableArray<Type>> _interfaceImplementationCache = new();

    // This field is not readonly because we use two-phase initialization to resolve the problem of cyclic dependencies.
    private ImmutableDictionary<Type, ServiceNode> _services;
    private Dictionary<Type, ServiceNode>? _servicesFast;

    // A collection of services that is shared by all instances derived from a root instance.
    // That is, adding a shared service to any instance of the same family will add the service to all instances of the family.
    private ConcurrentDictionary<Type, ServiceNode> _sharedServices;

    // Make sure to return a new instance at each call to avoid unwanted shared services.
    public static ServiceProvider<TBase> Empty => new();

    private ServiceProvider() : this( ImmutableDictionary<Type, ServiceNode>.Empty, null, new ConcurrentDictionary<Type, ServiceNode>() ) { }

    private ServiceProvider<TBase> Clone( ImmutableDictionary<Type, ServiceNode> services, IServiceProvider? nextProvider )
    {
        var clone = (ServiceProvider<TBase>) this.MemberwiseClone();
        clone._services = services;
        clone._servicesFast = null;
        clone.NextProvider = nextProvider;

        return clone;
    }

    private ServiceProvider(
        ImmutableDictionary<Type, ServiceNode> services,
        IServiceProvider? nextProvider,
        ConcurrentDictionary<Type, ServiceNode> sharedServices )
    {
        this._services = services;
        this.NextProvider = nextProvider;
        this._sharedServices = sharedServices;
    }

    private ServiceProvider<TBase> WithServiceCore( ServiceNode serviceNode, bool allowOverride, bool isShared, bool disableCaching )
    {
        var implementedInterfaces = this.GetImplementedInterfaces( serviceNode.ServiceType, disableCaching );

        if ( isShared )
        {
            foreach ( var interfaceType in implementedInterfaces )
            {
#if DEBUG
                if ( !allowOverride )
                {
                    if ( this._services.TryGetValue( interfaceType, out var conflictingServiceNode ) )
                    {
                        ReportConfict( interfaceType, conflictingServiceNode );
                    }

                    if ( this.NextProvider?.GetService( interfaceType ) != null )
                    {
                        ReportConfict( interfaceType );
                    }
                }
#endif

                this._sharedServices.AddOrUpdate(
                    interfaceType,
                    serviceNode,
                    ( _, conflictingServiceNode ) =>
                    {
                        if ( !allowOverride )
                        {
                            ReportConfict( interfaceType, conflictingServiceNode );
                        }

                        return serviceNode;
                    } );
            }

            return this;
        }
        else
        {
            var builder = this._services.ToBuilder();

            foreach ( var interfaceType in implementedInterfaces )
            {
#if DEBUG
                if ( !allowOverride )
                {
                    if ( builder.TryGetValue( interfaceType, out var conflictingServiceNode ) )
                    {
                        ReportConfict( interfaceType, conflictingServiceNode );
                    }

                    if ( this.NextProvider?.GetService( interfaceType ) != null )
                    {
                        ReportConfict( interfaceType );
                    }
                }
#endif

                builder[interfaceType] = serviceNode;
            }

            return this.Clone( builder.ToImmutable(), this.NextProvider );
        }

        static void ReportConfict( Type interfaceType, ServiceNode? conflictingServiceNode = null )
        {
            var message = $"The service provider already contains the service '{interfaceType}'.";

#if DEBUG
            if ( conflictingServiceNode != null )
            {
                message += " The allocation stack trace is: " + Environment.NewLine + conflictingServiceNode.AllocationStackTrace + "--";
            }
#endif
            throw new InvalidOperationException( message );
        }
    }

    public ServiceProvider<TBase> WithUntypedService( Type interfaceType, object implementation, bool allowOverride = false )
    {
        var serviceNode = ServiceNode.CreateEager( interfaceType, implementation );

        var newServices =
            allowOverride ? this._services.SetItem( interfaceType, serviceNode ) : this._services.Add( interfaceType, serviceNode );

        return this.Clone( newServices, this.NextProvider );
    }

    private ImmutableArray<Type> GetImplementedInterfaces( Type serviceType, bool disableCaching )
    {
        if ( disableCaching || !_interfaceImplementationCache.TryGetValue( serviceType, out var implementedInterfaces ) )
        {
            var arrayBuilder = ImmutableArray.CreateBuilder<Type>();

            var interfaces = serviceType.GetInterfaces();

            foreach ( var interfaceType in interfaces )
            {
                if ( typeof(TBase).IsAssignableFrom( interfaceType ) && interfaceType != typeof(TBase) )
                {
                    arrayBuilder.Add( interfaceType );
                }
            }

            for ( var cursorType = serviceType;
                  cursorType != null && typeof(TBase).IsAssignableFrom( cursorType );
                  cursorType = cursorType.BaseType )
            {
                arrayBuilder.Add( cursorType );
            }

            implementedInterfaces = arrayBuilder.ToImmutable();

            if ( !disableCaching )
            {
                _interfaceImplementationCache[serviceType] = implementedInterfaces;
            }

            return implementedInterfaces;
        }

        return implementedInterfaces;
    }

    /// <summary>
    /// Returns a new <see cref="ServiceProvider{TBase}"/> where a service have been added to the current <see cref="ServiceProvider{TBase}"/>.
    /// If the new service is already present in the current <see cref="ServiceProvider{TBase}"/>, it is replaced in the new <see cref="ServiceProvider{TBase}"/>.
    /// </summary>
    public ServiceProvider<TBase> WithService( TBase service, bool allowOverride = false, bool disableCaching = false )
        => this.WithServiceCore( ServiceNode.CreateEager( service.GetType(), service ), allowOverride, false, disableCaching );

    public ServiceProvider<TBase> WithServiceConditional<T>( Func<ServiceProvider<TBase>, T> func, bool disableCaching = false )
        where T : class, TBase
    {
        if ( this.GetServiceNode( typeof(T) ) == null )
        {
            return this.WithService( func, disableCaching: disableCaching );
        }
        else
        {
            return this;
        }
    }

    public ServiceProvider<TBase> WithService<T>( Func<ServiceProvider<TBase>, T> func, bool allowOverride = false, bool disableCaching = false )
        where T : class, TBase
        => this.WithServiceCore( ServiceNode.CreateLazy( typeof(T), func ), allowOverride, false, disableCaching );

    public ServiceProvider<TBase> AddSharedService( TBase service )
        => this.WithServiceCore( ServiceNode.CreateEager( service.GetType(), service ), false, true, false );

    public ServiceProvider<TBase> AddSharedService<T>( Func<ServiceProvider<TBase>, T> func )
        where T : class, TBase
        => this.WithServiceCore( ServiceNode.CreateLazy( typeof(T), func ), false, true, false );

    object? IServiceProvider.GetService( Type serviceType ) => this.GetService( serviceType );

    /// <summary>
    /// Attempts to get a service if it has already been instantiated.
    /// </summary>
    public bool TryGetService<T>( [NotNullWhen( true )] out T? service ) where T : class, TBase
    {
        var serviceType = typeof(T);

        if ( !this._services.TryGetValue( serviceType, out var serviceNode ) ||
             !serviceNode.TryGetService( out var untypedService ) )
        {
            service = null;

            return false;
        }

        service = (T) untypedService;

#pragma warning disable CS8762 // Parameter must have a non-null value when exiting in some condition.
        return true;
#pragma warning restore CS8762 // Parameter must have a non-null value when exiting in some condition.
    }

    /// <summary>
    /// Gets the implementation of a given service type.
    /// </summary>
    public object? GetService( Type serviceType )
    {
        // We use the ImmutableDictionary to build the ServiceProvider, but to consume services, we use a Dictionary

        var serviceNode = this.GetServiceNode( serviceType );

        if ( serviceNode != null )
        {
            return serviceNode.GetService( this );
        }
        else
        {
            return this.NextProvider?.GetService( serviceType );
        }
    }

    private ServiceNode? GetServiceNode( Type serviceType )
    {
        // which has a O(1) access time instead of O(log(n)). The Dictionary will be used in a read-only manner only.
        // Data races in instantiating this Dictionary do not matter.
        this._servicesFast ??= new Dictionary<Type, ServiceNode>( this._services );

        if ( this._servicesFast!.TryGetValue( serviceType, out var serviceNode ) )
        {
            return serviceNode;
        }
        else if ( this._sharedServices.TryGetValue( serviceType, out serviceNode ) )
        {
            return serviceNode;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Returns a new <see cref="ServiceProvider{TBase}"/> where some given services have been added to the current <see cref="ServiceProvider{TBase}"/>.
    /// If some of the new services are already present in the current <see cref="ServiceProvider{TBase}"/>, they are replaced in the new <see cref="ServiceProvider{TBase}"/>.
    /// </summary>
    public ServiceProvider<TBase> WithServices( IEnumerable<TBase>? services, bool disableCaching = false )
    {
        if ( services == null )
        {
            return this;
        }

        var provider = this;

        foreach ( var s in services )
        {
            provider = provider.WithService( s, disableCaching: disableCaching );
        }

        return provider;
    }

    /// <summary>
    /// Returns a new <see cref="ServiceProvider{TBase}"/> where some given services have been added to the current <see cref="ServiceProvider{TBase}"/>.
    /// If some of the new services are already present in the current <see cref="ServiceProvider{TBase}"/>, they are replaced in the new <see cref="ServiceProvider{TBase}"/>.
    /// </summary>
    public ServiceProvider<TBase> WithServices( TBase service1, TBase service2, params TBase[] otherServices )
        => this.WithServices( [service1, service2, ..otherServices] );

    /// <summary>
    /// Sets or replaces the next service provider in a chain.
    /// </summary>
    /// <param name="nextProvider"></param>
    /// <remarks>
    /// When the current service provider fails to find a service, it will try to find it using the next provider in the chain.
    /// When the next service provider has been set before, it gets replaced.
    /// </remarks>
    internal ServiceProvider<TBase> WithNextProvider( IServiceProvider nextProvider ) => new( this._services, nextProvider, this._sharedServices );

    public T? GetService<T>()
        where T : class, TBase
        => (T?) this.GetService( typeof(T) );

    public override string ToString() => $"ServiceProvider Entries={this._services.Count}";

    public override void Dispose()
    {
        foreach ( var serviceNode in this._services.Values )
        {
            serviceNode.Dispose();
        }

        if ( this.NextProvider is IDisposable disposable )
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Returns a <see cref="ServiceProvider{TBase}"/> that has different shared services than the current <see cref="ServiceProvider{TBase}"/>.
    /// The new <see cref="ServiceProvider{TBase}"/> inherits all shared services of the current one, but adding a new shared service to the new
    /// instance will not affect the old instance, and adding services to the old instance will not affect the new one.
    /// </summary>
    public ServiceProvider<TBase> WithDisjointSharedServices()
        => new( this._services, this.NextProvider, new ConcurrentDictionary<Type, ServiceNode>( this._sharedServices ) );
}