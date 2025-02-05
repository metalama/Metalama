// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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