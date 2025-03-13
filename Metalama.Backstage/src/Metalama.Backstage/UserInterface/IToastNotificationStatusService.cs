// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.UserInterface;

public interface IToastNotificationStatusService : IBackstageService
{
    /// <summary>
    /// Tries to acquire the right to display a notification, and updates the snooze period.
    /// </summary>
    bool TryAcquire( ToastNotificationKind kind );

    void Snooze( ToastNotificationKind kind );

    void Mute( ToastNotificationKind kind );

    /// <summary>
    /// Pause all notifications. This is used when the VSX setup wizard is scheduled or open, and
    /// should take care of activation. This method returns a cookie that must be disposed to resume operations.
    /// </summary>
    /// <param name="timeSpan">The <see cref="TimeSpan"/> during which notifications should be paused.</param>
    IDisposable PauseAll( TimeSpan timeSpan );
}