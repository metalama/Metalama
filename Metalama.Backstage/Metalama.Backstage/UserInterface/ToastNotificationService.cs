// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.UserInterface;

internal class ToastNotificationService : IToastNotificationService
{
    private readonly IToastNotificationStatusService _toastNotificationStatusService;
    private readonly IUserInterfaceService _userInterfaceService;
    private readonly ILogger _logger;

    public ToastNotificationService( IServiceProvider serviceProvider )
    {
        this._toastNotificationStatusService = serviceProvider.GetRequiredBackstageService<IToastNotificationStatusService>();
        this._userInterfaceService = serviceProvider.GetRequiredBackstageService<IUserInterfaceService>();
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( this.GetType().Name );
    }

    public bool Show( ToastNotification notification )
    {
        this._logger.Trace?.Log( $"Received a request to display the notification: {notification}." );

        if ( this._toastNotificationStatusService.TryAcquire( notification.Kind ) )
        {
            var notified = false;

            this._logger.Trace?.Log( $"Displaying the notification." );
            this._userInterfaceService.ShowToastNotification( notification, ref notified );

            return true;
        }
        else
        {
            this._logger.Trace?.Log( $"The notification of kind {notification.Kind.Name} was not displayed because it was snoozed or muted." );

            return false;
        }
    }
}