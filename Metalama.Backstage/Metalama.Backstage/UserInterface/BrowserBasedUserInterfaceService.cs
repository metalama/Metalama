// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

    public override void ShowToastNotification( ToastNotification notification )
    {
        if ( notification.Kind == ToastNotificationKinds.RequiresLicense )
        {
            this._logger.Trace?.Log( "Starting the setup UI." );

            // We are waiting for the method to complete because we have no mechanism to ensure that the process does
            // not end before the method completes.
            Task.Run( () => this.OpenConfigurationWebPageAsync( "Setup" ) ).Wait();
            this.OnToastNotificationShown();
        }
        else
        {
            this._logger.Trace?.Log( $"Ignoring a notification of kind {notification.Kind.Name}." );
        }
    }
}