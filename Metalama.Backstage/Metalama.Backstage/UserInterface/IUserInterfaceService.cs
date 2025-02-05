// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using System.Threading.Tasks;

namespace Metalama.Backstage.UserInterface;

public interface IUserInterfaceService : IBackstageService
{
    void OpenExternalWebPage( string url, BrowserMode browserMode );

    Task OpenConfigurationWebPageAsync( string path );

    /// <summary>
    /// Shows a toast notification. This method does not take the mute and snooze status into account.
    /// This is the job of the <see cref="IToastNotificationService"/>.
    /// </summary>
    void ShowToastNotification( ToastNotification notification, ref bool notificationReported );
}

public enum BrowserMode
{
    Default,
    NewWindow,
    Application
}