// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading;

namespace Metalama.Backstage.Diagnostics;

internal abstract class SimpleLoggerFactory : ILoggerFactory
{
    private readonly ImmutableHashSet<string> _traceCategories;
    private readonly bool _includeThreadId;
    private readonly string _productName;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleLoggerFactory"/> class.
    /// </summary>
    /// <param name="productName">The name of the product, which prefixes every record.</param>
    /// <param name="traceCategories">The categories for which trace records are written, or <c>*</c> for all of them.</param>
    /// <param name="includeThreadId">A value indicating whether the identifier of the current thread is written in every record.</param>
    protected SimpleLoggerFactory( string productName, ImmutableHashSet<string> traceCategories, bool includeThreadId = true )
    {
        this._productName = productName;
        this._traceCategories = traceCategories;
        this._includeThreadId = includeThreadId;
    }

    public ILogger GetLogger( string category ) => new Logger( this, category, category );

    public abstract void Flush();

    protected abstract void Write( string message );

    public IDisposable EnterScope( string scope ) => LoggingContext.EnterScope( scope );

    private sealed class Logger : ILogger
    {
        public SimpleLoggerFactory Factory { get; }

        public string Prefix { get; }

        private string Category { get; }

        public Logger( SimpleLoggerFactory factory, string prefix, string category )
        {
            this.Factory = factory;
            this.Prefix = prefix;
            this.Category = category;

            this.Error = this.CreateLogWriter( "ERROR" );
            this.Warning = this.CreateLogWriter( "WARNING" );
            this.Info = this.CreateLogWriter( "INFO" );

            if ( this.Factory._traceCategories.Contains( category ) || this.Factory._traceCategories.Contains( "*" ) )
            {
                this.Trace = this.CreateLogWriter( "TRACE" );
            }
        }

        public ILogWriter? Trace { get; }

        public ILogWriter? Info { get; }

        public ILogWriter? Warning { get; }

        public ILogWriter? Error { get; }

        public ILogger WithPrefix( string prefix )
            => new Logger(
                this.Factory,
                string.IsNullOrEmpty( this.Prefix ) ? prefix : this.Prefix + " - " + prefix,
                this.Category );

        private LogWriter CreateLogWriter( string logLevel ) => new( this, logLevel );
    }

    private sealed class LogWriter : ILogWriter
    {
        private readonly Logger _logger;
        private readonly string _logLevel;

        public LogWriter( Logger logger, string logLevel )
        {
            this._logger = logger;
            this._logLevel = logLevel;
        }

        public void Log( string message )
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append( "# " ).Append( this._logger.Factory._productName ).Append( ' ' );
            stringBuilder.Append( this._logLevel );

            var loggingContext = LoggingContext.Current;

            if ( loggingContext != null )
            {
                stringBuilder.Append( " " );
                stringBuilder.Append( loggingContext.Scope );
            }

            if ( this._logger.Factory._includeThreadId )
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

            this._logger.Factory.Write( stringBuilder.ToString() );
        }
    }
}