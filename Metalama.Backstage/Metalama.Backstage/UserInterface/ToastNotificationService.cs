// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.UserInterface;

internal sealed class ToastNotificationService : IToastNotificationService
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
            this._logger.Trace?.Log( $"Displaying the notification." );
            this._userInterfaceService.ShowToastNotification( notification );

            return true;
        }
        else
        {
            this._logger.Trace?.Log( $"The notification of kind {notification.Kind.Name} was not displayed because it was snoozed or muted." );

            return false;
        }
    }
}