// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
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

    public override void ShowToastNotification( ToastNotification notification, ref bool notificationReported )
    {
        if ( notification.Kind == ToastNotificationKinds.RequiresLicense )
        {
            this._logger.Trace?.Log( "Starting the setup UI." );

            // We are waiting for the method to complete because we have no mechanism to ensure that the process does
            // not end before the method completes.
            Task.Run( () => this.OpenConfigurationWebPageAsync( "Setup" ) ).Wait();
            notificationReported = true;
        }
        else
        {
            this._logger.Trace?.Log( $"Ignoring a notification of kind {notification.Kind.Name}." );
        }
    }
}