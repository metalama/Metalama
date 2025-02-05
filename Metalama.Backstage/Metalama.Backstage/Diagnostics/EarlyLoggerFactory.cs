// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Utilities;
using System;
using System.Collections.Concurrent;

namespace Metalama.Backstage.Diagnostics;

internal class EarlyLoggerFactory : ILoggerFactory
{
    private readonly ConcurrentDictionary<string, Logger> _loggers = new();

    public ILoggerFactory LoggerFactory { get; private set; }

    public EarlyLoggerFactory( ILoggerFactory loggerFactory )
    {
        this.LoggerFactory = loggerFactory;
    }

    public EarlyLoggerFactory()
    {
        this.LoggerFactory = new BufferingLoggerFactory();
    }

    public void Replace( ILoggerFactory loggerFactory )
    {
        if ( this.LoggerFactory is not BufferingLoggerFactory bufferingLoggerFactory )
        {
            // It has already been replaced.
            return;
        }

        bufferingLoggerFactory.Replay( loggerFactory );
        this.LoggerFactory = loggerFactory;

        foreach ( var logger in this._loggers )
        {
            logger.Value.Underlying = loggerFactory.GetLogger( logger.Key );
        }
    }

    public ILogger GetLogger( string category ) => this._loggers.GetOrAdd( category, c => new Logger( this.LoggerFactory.GetLogger( c ) ) );

    public void Flush() { }

    public IDisposable EnterScope( string scope ) => default(DisposableAction);

    private class Logger : ILogger
    {
        public ILogger Underlying { get; set; }

        public Logger( ILogger underlying )
        {
            this.Underlying = underlying;
        }

        public ILogWriter? Trace => this.Underlying.Trace;

        public ILogWriter? Info => this.Underlying.Info;

        public ILogWriter? Warning => this.Underlying.Warning;

        public ILogWriter? Error => this.Underlying.Error;

        public ILogger WithPrefix( string prefix ) => this.Underlying.WithPrefix( prefix );
    }
}