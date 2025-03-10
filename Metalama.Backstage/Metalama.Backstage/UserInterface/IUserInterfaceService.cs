// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using System;
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

    DateTime? LastToastNotificationTime { get; }
}

public enum BrowserMode
{
    Default,
    NewWindow,
    Application
}