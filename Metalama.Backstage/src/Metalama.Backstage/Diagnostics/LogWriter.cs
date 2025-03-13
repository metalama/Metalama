// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading;

namespace Metalama.Backstage.Diagnostics;

internal sealed class LogWriter : ILogWriter
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