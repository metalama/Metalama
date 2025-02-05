// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;

namespace Metalama.Backstage.Diagnostics;

internal class ConsoleLoggerFactory : ILoggerFactory
{
    public TextWriter TextWriter { get; }

    public ImmutableHashSet<string> TraceCategories { get; }

    public bool IncludeThreadId { get; }

    public ConsoleLoggerFactory( TextWriter textWriter, ImmutableHashSet<string> traceCategories, bool includeThreadId = true )
    {
        this.TextWriter = textWriter;
        this.TraceCategories = traceCategories;
        this.IncludeThreadId = includeThreadId;
    }

    public ILogger GetLogger( string category ) => new ConsoleLogger( this, category, category );

    public void Flush() => this.TextWriter.Flush();

    public IDisposable EnterScope( string scope ) => LoggingContext.EnterScope( scope );

    private class ConsoleLogger : ILogger
    {
        public ConsoleLoggerFactory Factory { get; }

        public string Prefix { get; }

        private string Category { get; }

        public ConsoleLogger( ConsoleLoggerFactory factory, string prefix, string category )
        {
            this.Factory = factory;
            this.Prefix = prefix;
            this.Category = category;

            this.Error = this.CreateLogWriter( "ERROR" );
            this.Warning = this.CreateLogWriter( "WARNING" );
            this.Info = this.CreateLogWriter( "INFO" );

            if ( this.Factory.TraceCategories.Contains( category ) || this.Factory.TraceCategories.Contains( "*" ) )
            {
                this.Trace = this.CreateLogWriter( "TRACE" );
            }
        }

        public ILogWriter? Trace { get; }

        public ILogWriter? Info { get; }

        public ILogWriter? Warning { get; }

        public ILogWriter? Error { get; }

        public ILogger WithPrefix( string prefix )
            => new ConsoleLogger(
                this.Factory,
                string.IsNullOrEmpty( this.Prefix ) ? prefix : this.Prefix + " - " + prefix,
                this.Category );

        private ConsoleLogWriter? CreateLogWriter( string logLevel ) => new( this, logLevel );
    }

    private class ConsoleLogWriter : ILogWriter
    {
        private readonly ConsoleLogger _logger;
        private readonly string _logLevel;

        public ConsoleLogWriter( ConsoleLogger logger, string logLevel )
        {
            this._logger = logger;
            this._logLevel = logLevel;
        }

        public void Log( string message )
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append( "# Metalama " );
            stringBuilder.Append( this._logLevel );

            var loggingContext = LoggingContext.Current;

            if ( loggingContext != null )
            {
                stringBuilder.Append( " " );
                stringBuilder.Append( loggingContext.Scope );
            }

            if ( this._logger.Factory.IncludeThreadId )
            {
                stringBuilder.Append( ", Thread " );
                stringBuilder.Append( Thread.CurrentThread.ManagedThreadId );
            }

            if ( !string.IsNullOrEmpty( this._logger.Prefix ) )
            {
                stringBuilder.Append( ", " );
                stringBuilder.Append( this._logger.Prefix );
            }

            stringBuilder.Append( ": " );
            stringBuilder.Append( message );

            var textWriter = this._logger.Factory.TextWriter;

            lock ( textWriter )
            {
                textWriter.WriteLine( stringBuilder.ToString() );
            }
        }
    }
}