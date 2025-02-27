// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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