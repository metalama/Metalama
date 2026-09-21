// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.UserInterface.Toasts;
using Xunit;

namespace Metalama.Backstage.Tests.UserInterface;

/// <summary>
/// Tests the decision of <see cref="ToastNotificationSupport"/>, given the facts that it reads from the operating
/// system. See issue #2047.
/// </summary>
public sealed class ToastNotificationSupportTests
{
    private const int _serviceStartModeAutomatic = 2;
    private const int _serviceStartModeDisabled = 4;

    /// <summary>
    /// Asserts that a toast notification is considered displayable only on Windows, only when the Windows Push
    /// Notifications User Service is installed and enabled, and only when neither the user nor a policy disabled
    /// toast notifications. A registry value that is absent carries no information and does not disable anything.
    /// </summary>
    /// <param name="isWindows">Whether the process runs on Windows.</param>
    /// <param name="wpnUserServiceStartMode">The start mode of the Windows Push Notifications User Service.</param>
    /// <param name="toastEnabled">The <c>ToastEnabled</c> setting of the current user.</param>
    /// <param name="noToastApplicationNotification">The <c>NoToastApplicationNotification</c> policy.</param>
    /// <param name="expected">The expected decision.</param>
    [Theory]

    // A machine with the notification platform installed and no setting that disables notifications.
    [InlineData( true, _serviceStartModeAutomatic, null, null, true )]
    [InlineData( true, _serviceStartModeAutomatic, 1, 0, true )]

    // The process does not run on Windows.
    [InlineData( false, _serviceStartModeAutomatic, null, null, false )]

    // The notification platform is not installed, which is the case on a Windows installation without the desktop
    // experience. This is the condition reported as "the notification platform is unavailable".
    [InlineData( true, null, null, null, false )]

    // The Windows Push Notifications User Service is disabled.
    [InlineData( true, _serviceStartModeDisabled, null, null, false )]

    // The current user turned toast notifications off.
    [InlineData( true, _serviceStartModeAutomatic, 0, null, false )]

    // A policy forbids toast notifications.
    [InlineData( true, _serviceStartModeAutomatic, null, 1, false )]
    public void IsSupported( bool isWindows, int? wpnUserServiceStartMode, int? toastEnabled, int? noToastApplicationNotification, bool expected )
        => Assert.Equal(
            expected,
            ToastNotificationSupport.ComputeIsSupported( isWindows, wpnUserServiceStartMode, toastEnabled, noToastApplicationNotification ) );
}
