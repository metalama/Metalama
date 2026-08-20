// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.UserInterface.Toasts;

internal sealed class ToastNotificationService : IToastNotificationService
{
    private readonly IToastNotificationStatusService _toastNotificationStatusService;
    private readonly IUserInterfaceService _userInterfaceService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly IUserDeviceDetectionService _userDeviceDetectionService;
    private readonly IApplicationInfo _applicationInfo;

    public ToastNotificationService( IServiceProvider serviceProvider )
    {
        this._toastNotificationStatusService = serviceProvider.GetRequiredBackstageService<IToastNotificationStatusService>();
        this._userInterfaceService = serviceProvider.GetRequiredBackstageService<IUserInterfaceService>();
        this._loggerFactory = serviceProvider.GetLoggerFactory();
        this._logger = this._loggerFactory.GetLogger( nameof(ToastNotificationService) );
        this._userDeviceDetectionService = serviceProvider.GetRequiredBackstageService<IUserDeviceDetectionService>();
        this._applicationInfo = serviceProvider.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication;
    }

    public bool Show( ToastNotification notification )
    {
        this._logger.Trace?.Log( $"Received a request to display the notification: {notification}." );

        // A notification asks a user to act. An unattended process, typically a continuous integration build, has no
        // user to read it, and the tool that displays the notification is usually not installed on such a machine.
        // See issue #1859.
        if ( this._applicationInfo.IsUnattendedProcess( this._loggerFactory ) )
        {
            this._logger.Trace?.Log( $"The notification of kind {notification.Kind.Name} was not displayed because the process is unattended." );

            return false;
        }

        if ( !this._userDeviceDetectionService.IsInteractiveDevice )
        {
            this._logger.Trace?.Log( $"The notification of kind {notification.Kind.Name} was not displayed because the current session is not interactive." );

            return false;
        }

        if ( this._toastNotificationStatusService.TryAcquire( notification.Kind ) )
        {
            this._logger.Trace?.Log( $"Displaying the notification using {this._userInterfaceService}." );
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
