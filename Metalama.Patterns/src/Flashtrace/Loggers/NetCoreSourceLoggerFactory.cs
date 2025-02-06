// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Records;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Flashtrace.Loggers;

internal sealed class NetCoreSourceLoggerFactory : IFlashtraceLoggerFactory
{
    private readonly ILoggerFactory _underlyingLoggerFactory;
    private readonly ConcurrentDictionary<FlashtraceRole, IFlashtraceRoleLoggerFactory> _roleLoggerFactories = new();

    public NetCoreSourceLoggerFactory( ILoggerFactory underlyingLoggerFactory )
    {
        this._underlyingLoggerFactory = underlyingLoggerFactory;
    }

    public IFlashtraceRoleLoggerFactory ForRole( FlashtraceRole role )
    {
        if ( this._roleLoggerFactories.TryGetValue( role, out var factory ) )
        {
            return factory;
        }

        return this._roleLoggerFactories.GetOrAdd(
            role,
            _ => new RoleLoggerFactory( this._underlyingLoggerFactory, role ) );
    }

    private sealed class RoleLoggerFactory : IFlashtraceRoleLoggerFactory
    {
        public ILoggerFactory UnderlyingLoggerFactory { get; }

        private readonly ConcurrentDictionary<string, Logger> _loggers = new();
        private readonly FlashtraceRole _role;

        public RoleLoggerFactory( ILoggerFactory underlyingLoggerFactory, FlashtraceRole role )
        {
            this.UnderlyingLoggerFactory = underlyingLoggerFactory;
            this._role = role;
        }

        public IFlashtraceLogger GetLogger( Type type ) => this.GetLogger( type.FullName! );

        public IFlashtraceLogger GetLogger( string sourceName )
        {
            if ( this._loggers.TryGetValue( sourceName, out var logger ) )
            {
                return logger;
            }

            return this._loggers.GetOrAdd( sourceName, s => new Logger( this._role, s, this ) );
        }
    }

    private sealed class Logger : SimpleFlashtraceLogger
    {
        private readonly ILogger _underlyingLogger;

        public Logger( FlashtraceRole role, string name, RoleLoggerFactory factory ) : base( role, name )
        {
            this._underlyingLogger = factory.UnderlyingLoggerFactory.CreateLogger( this.Category );
            this.Factory = factory;
        }

        public override bool IsEnabled( FlashtraceLevel level ) => this._underlyingLogger.IsEnabled( level.ToLogLevel() );

        public override IFlashtraceRoleLoggerFactory Factory { get; }

        protected override void Write( FlashtraceLevel level, LogRecordKind recordKind, string text, Exception? exception )
        {
            this._underlyingLogger.Log( level.ToLogLevel(), exception, text );
        }
    }
}