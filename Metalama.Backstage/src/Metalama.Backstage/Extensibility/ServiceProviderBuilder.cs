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

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProviderBuilder"/> class backed by an arbitrary implementation of <see cref="IServiceProvider"/>.
        /// </summary>
        public ServiceProviderBuilder( Action<Type, Func<IServiceProvider, object>> addService )
        {
            this._addService = addService;
        }

        public void AddService( Type type, object instance ) => this._addService( type, _ => instance );

        public void AddService( Type type, Func<IServiceProvider, object> func ) => this._addService( type, func );
    }
}