// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.UserInterface.Toasts;
using System;
using System.Threading.Tasks;

namespace Metalama.Backstage.UserInterface;

internal class BrowserBasedUserInterfaceService : UserInterfaceService
{
    private readonly ILogger _logger;

    public BrowserBasedUserInterfaceService( IServiceProvider serviceProvider ) : base( serviceProvider )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( this.GetType().Name );
    }

    public override void ShowToastNotification( ToastNotification notification )
    {
        if ( notification.Kind == ToastNotificationKinds.RequiresLicense )
        {
            this._logger.Trace?.Log( "Starting the setup UI." );

            try
            {
                // We are waiting for the method to complete because we have no mechanism to ensure that the process does
                // not end before the method completes. GetResult rethrows the original exception, whereas Wait would
                // wrap it in an AggregateException whose message does not name the failure.
                Task.Run( () => this.OpenConfigurationWebPageAsync( "Setup" ) ).GetAwaiter().GetResult();
            }
            catch ( Exception e )
            {
                // A machine that has no Backstage Worker tool, typically a continuous integration agent, cannot open
                // the setup page. The caller is usually an MSBuild task that has just reported a licensing diagnostic,
                // and a failure to display a notification must not turn that diagnostic into a task failure. The
                // exception is therefore logged and not rethrown, even when recoverable exceptions are not ignored.
                // The whole exception is logged, including its stack trace, because this is the only record of it.
                // See issue #1859.
                this._logger.Error?.Log( $"Cannot open the setup web page: {e}" );
            }
        }
        else
        {
            this._logger.Trace?.Log( $"Ignoring a notification of kind {notification.Kind.Name}." );
        }
    }
}
