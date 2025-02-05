// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.Build.Framework;
using ILogger = Metalama.Backstage.Diagnostics.ILogger;

namespace Metalama.Framework.Workspaces;

internal sealed class MSBuildLogger : Microsoft.Build.Framework.ILogger
{
    private readonly ILogger _logger;

    public MSBuildLogger( ILogger logger )
    {
        this._logger = logger;
    }

    public void Initialize( IEventSource eventSource )
    {
        eventSource.MessageRaised += this.OnMessageRaised;
        eventSource.WarningRaised += this.OnWarningRaised;
        eventSource.ErrorRaised += this.OnErrorRaised;
    }

    private void OnMessageRaised( object sender, BuildMessageEventArgs e )
    {
        if ( e.Importance == MessageImportance.Low )
        {
            this._logger.Trace?.Log( $"{e.Code} {e.Message}" );
        }
        else
        {
            this._logger.Info?.Log( $"{e.Code} {e.Message}" );
        }
    }

    private void OnWarningRaised( object sender, BuildWarningEventArgs e )
    {
        this._logger.Warning?.Log( $"{e.Code} {e.Message}" );
    }

    private void OnErrorRaised( object sender, BuildErrorEventArgs e )
    {
        this._logger.Error?.Log( $"{e.Code} {e.Message}" );
    }

    public void Shutdown() { }

    public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Normal;

    public string? Parameters { get; set; }
}