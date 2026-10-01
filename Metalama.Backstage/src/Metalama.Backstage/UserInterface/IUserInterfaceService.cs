// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.UserInterface.Toasts;
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
    void ShowToastNotification( ToastNotification notification );

    /// <summary>
    /// Gets a value indicating whether the current machine can display a toast notification.
    /// </summary>
    /// <remarks>
    /// The Windows notification platform declines to serve the process on a Windows installation that does not
    /// include the notification platform, in a session that has no interactive desktop, and when a policy disables
    /// notifications. When this property returns <c>false</c>,
    /// <see cref="IToastNotificationDetectionService"/> skips the detection and
    /// <see cref="IToastNotificationService"/> does not display any notification, because the notification platform
    /// would decline the call. See issue #2047.
    /// </remarks>
    bool AreToastNotificationsSupported { get; }
}

public enum BrowserMode
{
    Default,
    NewWindow,
    Application
}