// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;

namespace Metalama.Backstage.Extensibility;

/// <summary>
/// A <see cref="ServiceProviderBuilder"/> backed by a minimal service provider of singletons. The provider is
/// disposable: disposing it disposes, in the reverse order of their creation, the services that it created and that
/// implement <see cref="IDisposable"/>. The instances supplied to the builder are not owned by the provider and are
/// not disposed.
/// </summary>
[PublicAPI]
public sealed class SimpleServiceProviderBuilder : ServiceProviderBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleServiceProviderBuilder"/> class.
    /// </summary>
    public SimpleServiceProviderBuilder() : this( new ServiceProviderImpl() ) { }

    private SimpleServiceProviderBuilder( ServiceProviderImpl impl ) : base( impl.AddService, impl.AddInstance )
    {
        this.ServiceProvider = impl;
    }

    /// <summary>
    /// Gets the service provider. It implements <see cref="IDisposable"/>.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    private sealed class ServiceProviderImpl : IServiceProvider, IDisposable
    {
        private readonly Dictionary<Type, Node> _nodes = [];
        private readonly List<IDisposable> _ownedDisposables = [];
        private readonly object _ownedDisposablesLock = new();
        private bool _isDisposed;

        public void AddService( Type serviceType, Func<IServiceProvider, object> func )
        {
            this._nodes[serviceType] = new Node( func, this, owned: true );
        }

        public void AddInstance( Type serviceType, object instance )
        {
            this._nodes[serviceType] = new Node( _ => instance, this, owned: false );
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

        private void OnServiceCreated( object instance )
        {
            if ( instance is IDisposable disposable )
            {
                lock ( this._ownedDisposablesLock )
                {
                    this._ownedDisposables.Add( disposable );
                }
            }
        }

        public void Dispose()
        {
            IDisposable[] disposables;

            lock ( this._ownedDisposablesLock )
            {
                if ( this._isDisposed )
                {
                    return;
                }

                this._isDisposed = true;
                disposables = this._ownedDisposables.ToArray();
                this._ownedDisposables.Clear();
            }

            // The services are disposed in the reverse order of their creation, so that a service is disposed before
            // the services it depends on.
            for ( var i = disposables.Length - 1; i >= 0; i-- )
            {
                disposables[i].Dispose();
            }
        }

        private sealed class Node
        {
            private readonly Func<IServiceProvider, object> _func;
            private readonly ServiceProviderImpl _owner;
            private readonly bool _owned;
            private object? _instance;

            public Node( Func<IServiceProvider, object> func, ServiceProviderImpl owner, bool owned )
            {
                this._func = func;
                this._owner = owner;
                this._owned = owned;
            }

            public object GetInstance( IServiceProvider serviceProvider )
            {
                if ( this._instance == null )
                {
                    lock ( this )
                    {
                        if ( this._instance == null )
                        {
                            this._instance = this._func( serviceProvider );

                            if ( this._owned )
                            {
                                this._owner.OnServiceCreated( this._instance );
                            }
                        }
                    }
                }

                return this._instance;
            }
        }
    }
}
