// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.Build.Framework;
using ILogger = Metalama.Backstage.Diagnostics.ILogger;

namespace Metalama.Framework.Workspaces;

internal sealed class MSBuildLogger( ILogger logger ) : Microsoft.Build.Framework.ILogger
{
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
            logger.Trace?.Log( $"{e.Code} {e.Message}" );
        }
        else
        {
            logger.Info?.Log( $"{e.Code} {e.Message}" );
        }
    }

    private void OnWarningRaised( object sender, BuildWarningEventArgs e )
    {
        logger.Warning?.Log( $"{e.Code} {e.Message}" );
    }

    private void OnErrorRaised( object sender, BuildErrorEventArgs e )
    {
        logger.Error?.Log( $"{e.Code} {e.Message}" );
    }

    public void Shutdown() { }

    public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Normal;

    public string? Parameters { get; set; }
}