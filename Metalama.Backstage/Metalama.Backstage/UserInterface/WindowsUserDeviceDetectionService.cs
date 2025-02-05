// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Utilities;
using System;

#if NETCOREAPP || NETFRAMEWORK
using Microsoft.Win32;
using System.Globalization;
#endif

using System.Runtime.InteropServices;

// ReSharper disable IdentifierTypo
// ReSharper disable UnusedMember.Local
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
#pragma warning disable SA1401

namespace Metalama.Backstage.UserInterface;

internal class WindowsUserDeviceDetectionService : IUserDeviceDetectionService
{
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;

    public WindowsUserDeviceDetectionService( IServiceProvider serviceProvider )
    {
        this._loggerFactory = serviceProvider.GetLoggerFactory();
        this._logger = this._loggerFactory.GetLogger( nameof(WindowsUserDeviceDetectionService) );
    }

    [StructLayout( LayoutKind.Sequential )]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }

    // Importing GetLastInputInfo from User32.dll
    [DllImport( "User32.dll" )]
    private static extern bool GetLastInputInfo( ref LastInputInfo lastInputInfo );

    private delegate bool MonitorEnumDelegate( IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData );

    [DllImport( "user32.dll" )]
    private static extern bool EnumDisplayMonitors( IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData );

    [DllImport( "user32.dll", SetLastError = true )]
    private static extern bool GetMonitorInfo( IntPtr hMonitor, ref MonitorInfo lpmi );

    [StructLayout( LayoutKind.Sequential )]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Auto )]
    private class MonitorInfo
    {
        public int Size = Marshal.SizeOf( typeof(MonitorInfo) );
        public Rect Monitor;
        public Rect WorkArea;
        public uint Flags;
    }

    private static int? GetTotalMonitorWidth()
    {
        try
        {
            if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
            {
                var totalWidth = 0;

                EnumDisplayMonitors(
                    IntPtr.Zero,
                    IntPtr.Zero,
                    ( IntPtr _, IntPtr _, ref Rect rect, IntPtr _ ) =>
                    {
                        totalWidth += rect.Right - rect.Left;

                        return true;
                    },
                    IntPtr.Zero );

                return totalWidth;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    // Method to get the last input time in seconds
    private static TimeSpan? GetLastInputTime()
    {
        try
        {
            if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
            {
                var lastInputInfo = default(LastInputInfo);
                lastInputInfo.Size = (uint) Marshal.SizeOf( lastInputInfo );

                if ( GetLastInputInfo( ref lastInputInfo ) )
                {
                    return TimeSpan.FromMilliseconds( (uint) Environment.TickCount - lastInputInfo.Time );
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public bool IsInteractiveDevice
    {
        get
        {
            if ( ProcessUtilities.IsCurrentProcessUnattended( this._loggerFactory ) )
            {
                return false;
            }

            // To reduce the chance of opening a UI in an unattended virtual machine, we
            // don't open it if there has been no recent user interaction. We also skip monitors
            // smaller than 1280 pixels since this is likely to be an unattended or test VM.

            var hasRecentUserInput = GetLastInputTime() is null or { TotalMinutes: < 15 };
            var hasLargeMonitor = GetTotalMonitorWidth() is null or >= 1280;

            this._logger.Trace?.Log( $"HasRecentUserInput={hasRecentUserInput}, HasLargeMonitor={hasLargeMonitor}" );

            return hasRecentUserInput && hasLargeMonitor;
        }
    }

    public bool? IsVisualStudioInstalled
    {
        get
        {
#if NETCOREAPP || NETFRAMEWORK
#pragma warning disable CA1416
            using var key = Registry.LocalMachine.OpenSubKey( @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio" );

            if ( key == null )
            {
                return false;
            }

            foreach ( var keyName in key.GetSubKeyNames() )
            {
                if ( decimal.TryParse( keyName, NumberStyles.Any, CultureInfo.InvariantCulture, out var version ) && version >= 17 )
                {
                    return true;
                }
            }
#pragma warning restore CA1416

            return false;

#else
#pragma warning disable IDE0025
            return null;
#pragma warning restore IDE0025
#endif
        }
    }
}