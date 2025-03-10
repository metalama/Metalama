// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Diagnostics;

namespace Metalama.Framework.Engine.Services;

public sealed partial class ServiceProvider<TBase>
{
    private sealed class ServiceNode
    {
        private readonly Func<ServiceProvider<TBase>, object>? _func;
        private object? _service;

#if DEBUG
        public StackTrace AllocationStackTrace { get; } = new();
#endif

        public Type ServiceType { get; }

        public ServiceNode( Type serviceType, object? service, Func<ServiceProvider<TBase>, object>? func )
        {
            this._func = func;
            this._service = service;
            this.ServiceType = serviceType;
        }

        public static ServiceNode CreateLazy( Type serviceType, Func<ServiceProvider<TBase>, object> func ) => new( serviceType, null, func );

        public static ServiceNode CreateEager( Type serviceType, object service ) => new( serviceType, service, null );

        public object GetService( ServiceProvider<TBase> serviceProvider )
        {
            if ( this._service == null )
            {
                lock ( this )
                {
                    this._service ??= this._func( serviceProvider );
                }
            }

            return this._service;
        }

        public bool TryGetService( out object? service )
        {
            service = this._service;

            return service != null;
        }

        public void Dispose()
        {
            var disposable = this._service as IDisposable;
            disposable?.Dispose();
        }
    }
}