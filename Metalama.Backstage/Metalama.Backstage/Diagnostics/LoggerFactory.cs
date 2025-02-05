// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Maintenance;
using System;
using System.Collections.Concurrent;

namespace Metalama.Backstage.Diagnostics
{
    internal class LoggerFactory : ILoggerFactory
    {
        private readonly ConcurrentDictionary<string, ILogger> _loggers = new( StringComparer.OrdinalIgnoreCase );
        private readonly ConcurrentDictionary<string, LogFileWriter> _logFileWriters = new();
        private readonly ITempFileManager _tempFileManager;

        public LoggerFactory(
            IServiceProvider serviceProvider,
            DiagnosticsConfiguration configuration,
            ProcessKind processKind )
        {
            this._tempFileManager = serviceProvider.GetRequiredBackstageService<ITempFileManager>();
            
            this.DateTimeProvider = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
            this.FileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();

            this.Configuration = configuration;
            this.ProcessKind = processKind;

            this.ShouldLogWarningsAndInfos = configuration.Logging.Processes.TryGetValue( processKind.ToString(), out var enabled ) && enabled;
        }

        internal bool ShouldLogWarningsAndInfos { get; }

        internal DiagnosticsConfiguration Configuration { get; }

        internal IDateTimeProvider DateTimeProvider { get; }

        internal IFileSystem FileSystem { get; }

        public ProcessKind ProcessKind { get; }
        
        public string GetLogDirectory() => this._tempFileManager.GetTempDirectory( "Logs", CleanUpStrategy.Always );

        public IDisposable EnterScope( string scope ) => LoggingContext.EnterScope( scope, this.CloseScope );

        public ILogger GetLogger( string category )
        {
            if ( this._loggers.TryGetValue( category, out var logger ) )
            {
                return logger;
            }
            else
            {
                return this._loggers.GetOrAdd( category, c => new Logger( this, c ) );
            }
        }

        public LogFileWriter GetLogFileWriter() => this.GetLogFileWriter( LoggingContext.Current?.Scope ?? "" );

        private LogFileWriter GetLogFileWriter( string scope )
        {
            if ( !this._logFileWriters.TryGetValue( scope, out var writer ) )
            {
                writer = this._logFileWriters.GetOrAdd( scope, s => new LogFileWriter( this, s ) );
            }

            return writer;
        }

        private void CloseScope( string name )
        {
            if ( this._logFileWriters.TryRemove( name, out var file ) )
            {
                file.Dispose();
            }
        }

        public void Flush()
        {
            foreach ( var file in this._logFileWriters )
            {
                file.Value.Flush();
            }
        }

        public void Close()
        {
            foreach ( var file in this._logFileWriters )
            {
                file.Value.Dispose();
            }

            this._logFileWriters.Clear();
        }
    }
}