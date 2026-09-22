// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;

namespace Metalama.Backstage.UserInterface.Toasts;

/// <summary>
/// Reports whether the current machine can display a Windows toast notification.
/// </summary>
/// <remarks>
/// The Windows notification platform declines to serve the process when the platform is not installed, when the
/// Windows Push Notifications User Service is disabled, when the current user turned toast notifications off, and
/// when a policy forbids them. The desktop notification tool receives such a refusal as a
/// <see cref="COMException"/>. <see cref="WindowsUserInterfaceService.AreToastNotificationsSupported"/> therefore
/// reports the value of <see cref="IsSupported"/>, which skips both the detection and the display of a toast
/// notification. See issue #2047.
/// </remarks>
internal static class ToastNotificationSupport
{
    private const string _pushNotificationsKeyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications";

    private const string _pushNotificationsPolicyKeyName = @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications";

    private const string _wpnUserServiceKeyName = @"SYSTEM\CurrentControlSet\Services\WpnUserService";

    /// <summary>
    /// The value of the <c>Start</c> registry value of a Windows service that is disabled.
    /// </summary>
    private const int _serviceStartModeDisabled = 4;

    private static readonly Lazy<bool> _isSupported = new( ReadIsSupported );

    /// <summary>
    /// Gets a value indicating whether the current machine can display a toast notification. The value is evaluated
    /// once per process because it reads the registry.
    /// </summary>
    public static bool IsSupported => _isSupported.Value;

    /// <summary>
    /// Decides whether a toast notification can be displayed, from the facts read from the operating system.
    /// </summary>
    /// <param name="isWindows"><c>true</c> when the process runs on Windows.</param>
    /// <param name="wpnUserServiceStartMode">
    /// The <c>Start</c> value of the Windows Push Notifications User Service, or <c>null</c> when the service is not
    /// installed. The service is absent on a Windows installation that does not include the notification platform.
    /// </param>
    /// <param name="toastEnabled">
    /// The <c>ToastEnabled</c> value of the current user, or <c>null</c> when the user did not set it.
    /// </param>
    /// <param name="noToastApplicationNotification">
    /// The <c>NoToastApplicationNotification</c> policy, or <c>null</c> when no policy is set.
    /// </param>
    /// <returns><c>true</c> when a toast notification can be displayed.</returns>
    internal static bool ComputeIsSupported( bool isWindows, int? wpnUserServiceStartMode, int? toastEnabled, int? noToastApplicationNotification )
        => isWindows
           && wpnUserServiceStartMode is not null and not _serviceStartModeDisabled
           && toastEnabled != 0
           && noToastApplicationNotification != 1;

    private static bool ReadIsSupported()
    {
        if ( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            return false;
        }

        return ComputeIsSupported(
            true,
            ReadRegistryValue( Registry.LocalMachine, _wpnUserServiceKeyName, "Start" ),
            ReadRegistryValue( Registry.CurrentUser, _pushNotificationsKeyName, "ToastEnabled" ),
            ReadRegistryValue( Registry.CurrentUser, _pushNotificationsPolicyKeyName, "NoToastApplicationNotification" )
            ?? ReadRegistryValue( Registry.LocalMachine, _pushNotificationsPolicyKeyName, "NoToastApplicationNotification" ) );
    }

    private static int? ReadRegistryValue( RegistryKey hive, string keyName, string valueName )
    {
        try
        {
#pragma warning disable CA1416
            using var key = hive.OpenSubKey( keyName );

            return key?.GetValue( valueName ) as int?;
#pragma warning restore CA1416
        }
        catch
        {
            // A registry value that cannot be read is reported as absent. This class runs before the logging services
            // are available, so there is nowhere to report the failure.
            return null;
        }
    }
}
