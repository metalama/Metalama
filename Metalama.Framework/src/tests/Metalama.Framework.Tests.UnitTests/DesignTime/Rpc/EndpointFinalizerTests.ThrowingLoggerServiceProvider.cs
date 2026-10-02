// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using SharpCrafters.Backstage.Diagnostics;
using System;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class EndpointFinalizerTests
{
    /// <summary>
    /// Service provider that returns an <see cref="ILoggerFactory"/> whose loggers throw when they write an error, and that
    /// delegates the other services to an underlying service provider.
    /// </summary>
    private sealed class ThrowingLoggerServiceProvider : IServiceProvider, ILoggerFactory, ILogger, ILogWriter
    {
        private readonly IServiceProvider _underlying;

        public ThrowingLoggerServiceProvider( IServiceProvider underlying )
        {
            this._underlying = underlying;
        }

        public object? GetService( Type serviceType ) => serviceType == typeof(ILoggerFactory) ? this : this._underlying.GetService( serviceType );

        public ILogger GetLogger( string category ) => this;

        public void Flush() { }

        public IDisposable EnterScope( string scope ) => throw new NotSupportedException();

        public ILogWriter? Trace => null;

        public ILogWriter? Info => null;

        public ILogWriter? Warning => null;

        public ILogWriter Error => this;

        public ILogger WithPrefix( string prefix ) => this;

        public void Log( string message ) => throw new InvalidOperationException( "Test exception thrown by the logger." );
    }
}
