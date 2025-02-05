// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Utilities;
using System;
using System.Collections.Concurrent;

namespace Metalama.Backstage.Diagnostics;

/// <summary>
/// A logger factory that buffers all log records and can then replay them on another <see cref="ILoggerFactory"/> by calling the <see cref="Replay(Metalama.Backstage.Diagnostics.ILoggerFactory)"/> method.
/// This class is used when the principal <see cref="ILoggerFactory"/> is not yet initialized.
/// </summary>
internal class BufferingLoggerFactory : ILoggerFactory
{
    private readonly ConcurrentQueue<Action<ILoggerFactory>> _replayActions = new();
    private readonly ConcurrentDictionary<string, ILogger> _loggers = new();

    // Always enabled because we don't have the configuration yet.

    public IDisposable EnterScope( string scope ) => default(DisposableAction);

    public ILogger GetLogger( string category ) => this._loggers.GetOrAdd( category, c => new Logger( this, c ) );

    public void Replay( ILoggerFactory loggerFactory )
    {
        foreach ( var action in this._replayActions )
        {
            action( loggerFactory );
        }
    }
    
    void ILoggerFactory.Flush() { }

    private class Logger : ILogger
    {
        private readonly BufferingLoggerFactory _parent;
        private readonly string _category;

        public Logger( BufferingLoggerFactory parent, string category )
        {
            this._parent = parent;
            this._category = category;
            this.Info = new Writer( parent, category, logger => logger.Info );
            this.Warning = new Writer( parent, category, logger => logger.Warning );
            this.Error = new Writer( parent, category, logger => logger.Error );
        }

        public ILogWriter? Trace => null;

        public ILogWriter? Info { get; }

        public ILogWriter? Warning { get; }

        public ILogWriter? Error { get; }

        public ILogger WithPrefix( string prefix ) => this._parent.GetLogger( this._category + "." + prefix );
    }

    private class Writer : ILogWriter
    {
        private readonly BufferingLoggerFactory _parent;
        private readonly string _category;
        private readonly Func<ILogger, ILogWriter?> _getWriter;

        public Writer( BufferingLoggerFactory parent, string category, Func<ILogger, ILogWriter?> getWriter )
        {
            this._parent = parent;
            this._category = category;
            this._getWriter = getWriter;
        }

        public void Log( string message )
            => this._parent._replayActions.Enqueue( loggerFactory => this._getWriter( loggerFactory.GetLogger( this._category ) )?.Log( message ) );
    }
}