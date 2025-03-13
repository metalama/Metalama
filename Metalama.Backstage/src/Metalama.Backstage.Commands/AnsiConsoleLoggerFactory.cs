// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Utilities;
using System;

namespace Metalama.Backstage.Commands;

internal sealed class AnsiConsoleLoggerFactory : ILoggerFactory, ILogger
{
    public AnsiConsoleLoggerFactory( ConsoleWriter consoleWriter, BaseCommandSettings settings )
    {
        this.Error = new LogWriter( consoleWriter.WriteError );

        if ( !settings.NoWarn )
        {
            this.Warning = new LogWriter( consoleWriter.WriteWarning );
        }

        this.Info = new LogWriter( consoleWriter.WriteImportantMessage );

        if ( settings.Verbose )
        {
            this.Trace = new LogWriter( consoleWriter.WriteMessage );
        }
    }

    public ILogger GetLogger( string category ) => this;

    void ILoggerFactory.Flush() { }

    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once UnusedParameter.Global
    public ILoggerFactory ForScope( string name ) => throw new NotSupportedException();

    public IDisposable EnterScope( string scope ) => default(DisposableAction);

    private class LogWriter : ILogWriter
    {
        private readonly Action<string> _action;

        public LogWriter( Action<string> action )
        {
            this._action = action;
        }

        public void Log( string message ) => this._action( message );
    }

    public ILogWriter? Trace { get; }

    public ILogWriter? Info { get; }

    public ILogWriter? Warning { get; }

    public ILogWriter? Error { get; }

    public ILogger WithPrefix( string prefix ) => this;
}