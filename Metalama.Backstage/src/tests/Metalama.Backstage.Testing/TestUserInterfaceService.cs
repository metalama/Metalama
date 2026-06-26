// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.UserInterface.Toasts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Backstage.Testing;

[PublicAPI]
public class TestUserInterfaceService : IUserInterfaceService
{
    private readonly ILogger _logger;

    public TestUserInterfaceService( IServiceProvider serviceProvider )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(TestUserInterfaceService) );
    }

    public List<ToastNotification> Notifications { get; } = [];

    public List<(string Url, BrowserMode Mode)> ExternalWebPagesOpened { get; } = [];

    public List<string> ConfigurationWebPagesOpened { get; } = [];

    public void OpenExternalWebPage( string url, BrowserMode browserMode ) => this.ExternalWebPagesOpened.Add( (url, browserMode) );

    public Task OpenConfigurationWebPageAsync( string path )
    {
        this._logger.Trace?.Log( $"Opening configuration web page: '{path}'." );

        lock ( this.ConfigurationWebPagesOpened )
        {
            this.ConfigurationWebPagesOpened.Add( path );
        }

        return Task.CompletedTask;
    }

    public void ShowToastNotification( ToastNotification notification )
    {
        this._logger.Trace?.Log( $"Received notification: {notification}." );

        lock ( this.Notifications )
        {
            this.Notifications.Add( notification );
        }
    }
}