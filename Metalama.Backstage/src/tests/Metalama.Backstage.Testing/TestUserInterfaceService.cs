// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.UserInterface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Backstage.Testing;

public class TestUserInterfaceService : IUserInterfaceService
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public TestUserInterfaceService( IServiceProvider serviceProvider )
    {
        this._dateTimeProvider = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
    }

    public List<ToastNotification> Notifications { get; } = [];

    public List<(string Url, BrowserMode Mode)> ExternalWebPagesOpened { get; } = [];

    public List<string> ConfigurationWebPagesOpened { get; } = [];

    public void OpenExternalWebPage( string url, BrowserMode browserMode ) => this.ExternalWebPagesOpened.Add( (url, browserMode) );

    public Task OpenConfigurationWebPageAsync( string path )
    {
        this.ConfigurationWebPagesOpened.Add( path );

        return Task.CompletedTask;
    }

    public void ShowToastNotification( ToastNotification notification )
    {
        this.Notifications.Add( notification );
        this.LastToastNotificationTime = this._dateTimeProvider.UtcNow;
    }

    public DateTime? LastToastNotificationTime { get; private set; }
}