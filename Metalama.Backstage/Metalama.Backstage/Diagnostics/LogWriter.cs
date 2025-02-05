// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Threading;

namespace Metalama.Backstage.Diagnostics;

internal class LogWriter : ILogWriter
{
    private readonly string _prefix;
    private readonly LoggerFactory _loggerFactory;
    private readonly string _logLevel;

    public LogWriter( Logger logger, string logLevel )
    {
        this._prefix = string.IsNullOrEmpty( logger.Prefix ) ? logger.Category : $"{logger.Category} - {logger.Prefix}";

        this._loggerFactory = logger.LoggerFactory;
        this._logLevel = logLevel.ToUpperInvariant();
    }

    public void Log( string message )
    {
        this._loggerFactory
            .GetLogFileWriter()
            .WriteLine(
                FormattableString.Invariant(
                    $"{this._loggerFactory.DateTimeProvider.UtcNow}, {this._logLevel}, Thread {Thread.CurrentThread.ManagedThreadId}, {this._prefix}: {message}" ) );
    }
}