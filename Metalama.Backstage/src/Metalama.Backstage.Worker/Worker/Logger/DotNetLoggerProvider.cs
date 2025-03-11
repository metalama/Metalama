// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using IDotNetLogger = Microsoft.Extensions.Logging.ILogger;
using IMetalamaLogger = Metalama.Backstage.Diagnostics.ILogger;

namespace Metalama.Backstage.Worker.Logger;

public class DotNetLoggerProvider : ILoggerProvider, IDotNetLogger
{
    private readonly IMetalamaLogger _logger;

    public DotNetLoggerProvider( IServiceProvider serviceProvider )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "dotnet" );
    }

    public void Dispose() { }

    public IDotNetLogger CreateLogger( string categoryName ) => this;

    public IDisposable BeginScope<TState>( TState state ) => new DotNetLoggerScope();

    public bool IsEnabled( LogLevel logLevel ) => this.GetLogWriter( logLevel ) != null;

    private ILogWriter? GetLogWriter( LogLevel logLevel )
        => logLevel switch
        {
            LogLevel.Critical => this._logger.Error,
            LogLevel.Debug => this._logger.Trace,
            LogLevel.Error => this._logger.Error,
            LogLevel.Information => this._logger.Info,
            LogLevel.None => null,
            LogLevel.Trace => this._logger.Trace,
            LogLevel.Warning => this._logger.Warning,
            _ => null
        };

    public void Log<TState>( LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter )
        => this.GetLogWriter( logLevel )?.Log( formatter( state, exception ) );

    private class DotNetLoggerScope : IDisposable
    {
        public void Dispose() { }
    }
}