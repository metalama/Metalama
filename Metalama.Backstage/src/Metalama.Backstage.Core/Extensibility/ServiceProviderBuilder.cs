// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Backstage.Extensibility
{
    /// <summary>
    /// Wraps a service provider factory or service collection, so that this project can
    /// register services in an arbitrary provider.
    /// </summary>
    [PublicAPI]
    public class ServiceProviderBuilder
    {
        private readonly Action<Type, Func<IServiceProvider, object>> _addService;
        private readonly Action<Type, object>? _addInstance;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProviderBuilder"/> class backed by an arbitrary implementation of <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="addService">A delegate that registers a service created by a factory.</param>
        public ServiceProviderBuilder( Action<Type, Func<IServiceProvider, object>> addService ) : this( addService, null ) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProviderBuilder"/> class backed by an arbitrary implementation of <see cref="IServiceProvider"/>,
        /// which distinguishes the services it creates from the instances that are supplied to it.
        /// </summary>
        /// <param name="addService">A delegate that registers a service created by a factory. The provider owns such a service and disposes it.</param>
        /// <param name="addInstance">A delegate that registers an existing instance, or <c>null</c> to register it as a service created by a factory. The provider does not own such an instance and does not dispose it.</param>
        public ServiceProviderBuilder( Action<Type, Func<IServiceProvider, object>> addService, Action<Type, object>? addInstance )
        {
            this._addService = addService;
            this._addInstance = addInstance;
        }

        /// <summary>
        /// Registers an existing instance. The provider does not dispose it, because it does not own it.
        /// </summary>
        public void AddService( Type type, object instance )
        {
            if ( this._addInstance != null )
            {
                this._addInstance( type, instance );
            }
            else
            {
                this._addService( type, _ => instance );
            }
        }

        /// <summary>
        /// Registers a service created by a factory. The provider owns the created service and disposes it, if it is
        /// disposable, when the provider itself is disposed.
        /// </summary>
        public void AddService( Type type, Func<IServiceProvider, object> func ) => this._addService( type, func );
    }
}
