// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Utilities;
using System;
using System.Collections.Concurrent;
using Xunit.Abstractions;

namespace Metalama.Testing.UnitTesting;

internal sealed class XunitLoggerFactory : ILoggerFactory
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly bool _verbose;
    private readonly ConcurrentDictionary<string, Logger> _loggers = new();

    public XunitLoggerFactory( ITestOutputHelper testOutputHelper, bool verbose = true )
    {
        this._testOutputHelper = testOutputHelper;
        this._verbose = verbose;
    }

    public ILogger GetLogger( string category )
        => this._loggers.GetOrAdd( category, static ( c, me ) => new Logger( c, me._testOutputHelper, me._verbose ), this );

    void ILoggerFactory.Flush() { }

    public IDisposable EnterScope( string scope ) => default(DisposableAction);

    private sealed class Logger : ILogger
    {
        private readonly string _prefix;
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly bool _verbose;

        public Logger( string prefix, ITestOutputHelper testOutputHelper, bool verbose )
        {
            this._prefix = prefix;
            this._testOutputHelper = testOutputHelper;
            this._verbose = verbose;

            if ( verbose )
            {
                this.Trace = new LogWriter( prefix, "TRACE", testOutputHelper );
            }

            this.Info = new LogWriter( prefix, "INFO", testOutputHelper );
            this.Warning = new LogWriter( prefix, "WARNING", testOutputHelper );
            this.Error = new LogWriter( prefix, "ERROR", testOutputHelper );
        }

        ILogger ILogger.WithPrefix( string prefix ) => new Logger( this._prefix + "." + prefix, this._testOutputHelper, this._verbose );

        public ILogWriter? Trace { get; }

        public ILogWriter? Info { get; }

        public ILogWriter? Warning { get; }

        public ILogWriter? Error { get; }
    }

    private sealed class LogWriter : ILogWriter
    {
        private readonly string _prefix;
        private readonly string _severity;
        private readonly ITestOutputHelper _testOutputHelper;

        public LogWriter( string prefix, string severity, ITestOutputHelper testOutputHelper )
        {
            this._prefix = prefix;
            this._severity = severity;
            this._testOutputHelper = testOutputHelper;
        }

        public void Log( string message )
        {
            try
            {
                this._testOutputHelper.WriteLine( $"{this._severity} {this._prefix}: {message}" );
            }
            catch ( InvalidOperationException ) { }
        }
    }
}