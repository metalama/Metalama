// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.Diagnostics;

internal class Logger : ILogger
{
    public LoggerFactory LoggerFactory { get; }

    public string Category { get; }

    public string Prefix { get; }

    public Logger( LoggerFactory loggerFactory, string category, string prefix = "" )
    {
        this.Prefix = prefix;
        this.LoggerFactory = loggerFactory;
        this.Category = category;
        this.Error = this.CreateLogWriter( "ERROR", true );
        this.Warning = this.CreateLogWriter( "WARNING", loggerFactory.ShouldLogWarningsAndInfos );
        this.Info = this.CreateLogWriter( "INFO", loggerFactory.ShouldLogWarningsAndInfos );

        this.Trace = this.CreateLogWriter(
            "TRACE",
            loggerFactory.ShouldLogWarningsAndInfos && this.LoggerFactory.Configuration.Logging.IsTraceCategoryEnabled( category ) );
    }

    private LogWriter? CreateLogWriter( string logLevel, bool isEnabled ) => isEnabled ? new LogWriter( this, logLevel ) : null;

    public ILogWriter? Trace { get; }

    public ILogWriter? Info { get; }

    public ILogWriter? Warning { get; }

    public ILogWriter? Error { get; }

    public ILogger WithPrefix( string prefix )
        => new Logger( this.LoggerFactory, this.Category, string.IsNullOrEmpty( this.Prefix ) ? prefix : this.Prefix + " - " + prefix );
}