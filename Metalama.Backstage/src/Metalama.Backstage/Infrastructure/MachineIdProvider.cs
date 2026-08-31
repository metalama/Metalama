// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Metalama.Backstage.Infrastructure;

/// <summary>
/// Reads the identifier of the current machine from the operating system.
/// </summary>
/// <remarks>
/// <para>
/// On Windows the value is the <c>MachineGuid</c> value of the <c>SOFTWARE\Microsoft\Cryptography</c> key of the
/// 32-bit view of the local machine registry hive. This is the value that PostSharp reads, and the 32-bit view is
/// part of the specification: the key is subject to registry redirection, so the 32-bit view and the 64-bit view can
/// hold different values on the same machine.
/// </para>
/// <para>
/// The other operating systems have no PostSharp implementation to be compatible with, so this class reads the
/// identifier that the operating system itself considers stable: <c>/etc/machine-id</c>, and then
/// <c>/var/lib/dbus/machine-id</c>, on Linux, and <c>IOPlatformUUID</c> on macOS.
/// </para>
/// <para>
/// When none of these values can be read, the class falls back to <see cref="Environment.MachineName"/>. That name
/// is stable, but it is not guaranteed to be unique, so a device count that includes such a machine is a lower
/// bound. A normally installed operating system never reaches this case.
/// </para>
/// </remarks>
internal sealed class MachineIdProvider : IMachineIdProvider
{
    private const string _windowsRegistryKeyName = @"SOFTWARE\Microsoft\Cryptography";
    private const string _windowsRegistryValueName = "MachineGuid";
    private const string _linuxMachineIdPath = "/etc/machine-id";
    private const string _linuxDBusMachineIdPath = "/var/lib/dbus/machine-id";

    private static readonly TimeSpan _macCommandTimeout = TimeSpan.FromSeconds( 10 );

    private readonly ILogger _logger;
    private readonly object _sync = new();

    private string? _machineId;

    public MachineIdProvider( IServiceProvider serviceProvider )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(MachineIdProvider) );
    }

    public string MachineId
    {
        get
        {
            // The value is read from the operating system, so it cannot change while the process runs, and reading it
            // costs a registry access or a child process. It is therefore read once per process.
            lock ( this._sync )
            {
                return this._machineId ??= this.ReadMachineId();
            }
        }
    }

    private string ReadMachineId()
    {
        string? machineId = null;

        try
        {
            if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
            {
                machineId = ReadWindowsMachineGuid();
            }
            else if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
            {
                machineId = ReadFirstLine( _linuxMachineIdPath ) ?? ReadFirstLine( _linuxDBusMachineIdPath );
            }
            else if ( RuntimeInformation.IsOSPlatform( OSPlatform.OSX ) )
            {
                machineId = this.ReadMacPlatformUuid();
            }
        }
        catch ( Exception e )
        {
            // The identifier is reported by telemetry, so no failure to read it may prevent the product from working.
            this._logger.Warning?.Log( $"Cannot read the machine identifier from the operating system: {e.Message}" );
        }

        if ( string.IsNullOrWhiteSpace( machineId ) )
        {
            // The machine name is stable, and it is the only value left that identifies the machine. It is not
            // guaranteed to be unique, so a device count computed from it is a lower bound.
            this._logger.Warning?.Log( "The operating system reports no machine identifier. Falling back to the machine name." );

            return Environment.MachineName;
        }

        return machineId!;
    }

    private static string? ReadWindowsMachineGuid()
    {
#pragma warning disable CA1416 // The call is guarded by a platform check.
        using var hive = RegistryKey.OpenBaseKey( RegistryHive.LocalMachine, RegistryView.Registry32 );
        using var key = hive.OpenSubKey( _windowsRegistryKeyName );

        return key?.GetValue( _windowsRegistryValueName ) as string;
#pragma warning restore CA1416
    }

    private static string? ReadFirstLine( string path )
    {
        if ( !File.Exists( path ) )
        {
            return null;
        }

        using var reader = new StreamReader( path );

        return reader.ReadLine()?.Trim();
    }

    private string? ReadMacPlatformUuid()
    {
        var startInfo = new ProcessStartInfo( "ioreg", "-rd1 -c IOPlatformExpertDevice" )
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
        };

        using var process = Process.Start( startInfo );

        if ( process == null )
        {
            return null;
        }

        // The error stream is drained asynchronously, otherwise a full error buffer would block the child process
        // while this method reads its output stream.
        process.ErrorDataReceived += ( _, _ ) => { };
        process.BeginErrorReadLine();

        var output = process.StandardOutput.ReadToEnd();

        if ( !process.WaitForExit( (int) _macCommandTimeout.TotalMilliseconds ) )
        {
            this._logger.Warning?.Log( "The 'ioreg' command did not complete in time." );

            return null;
        }

        var match = Regex.Match( output, @"""IOPlatformUUID""\s*=\s*""(?<uuid>[^""]+)""" );

        return match.Success ? match.Groups["uuid"].Value : null;
    }
}
