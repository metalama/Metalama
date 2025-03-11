// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;

namespace Metalama.Backstage.Extensibility;

public sealed class SimpleServiceProviderBuilder : ServiceProviderBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleServiceProviderBuilder"/> class.
    /// </summary>
    public SimpleServiceProviderBuilder() : this( new ServiceProviderImpl() ) { }

    private SimpleServiceProviderBuilder( ServiceProviderImpl impl ) : base( impl.AddService )
    {
        this.ServiceProvider = impl;
    }

    public IServiceProvider ServiceProvider { get; }

    private class ServiceProviderImpl : IServiceProvider
    {
        private readonly Dictionary<Type, Node> _nodes = [];

        public void AddService( Type serviceType, Func<IServiceProvider, object> func )
        {
            this._nodes[serviceType] = new Node( func );
        }

        public object? GetService( Type serviceType )
        {
            if ( this._nodes.TryGetValue( serviceType, out var node ) )
            {
                return node.GetInstance( this );
            }
            else
            {
                return null;
            }
        }

        private class Node
        {
            private readonly Func<IServiceProvider, object> _func;
            private object? _instance;

            public Node( Func<IServiceProvider, object> func )
            {
                this._func = func;
            }

            public object GetInstance( IServiceProvider serviceProvider )
            {
                if ( this._instance == null )
                {
                    lock ( this )
                    {
                        this._instance ??= this._func( serviceProvider );
                    }
                }

                return this._instance;
            }
        }
    }
}