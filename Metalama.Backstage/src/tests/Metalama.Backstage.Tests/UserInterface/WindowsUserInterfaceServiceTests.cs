// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Testing;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.UserInterface.Toasts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.UserInterface;

/// <summary>
/// Security regression tests for <c>WindowsUserInterfaceService</c>, which launches the desktop notification
/// tool as a child process. The notification title/text can originate from an untrusted RSS feed
/// (threat model: a hijacked/MITM'd metalama.net backend), so it must never be able to inject
/// additional command-line arguments into the child process (issue #1648).
/// </summary>
public sealed class WindowsUserInterfaceServiceTests : TestsBase
{
    public WindowsUserInterfaceServiceTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Verifies that a toast notification whose title contains a quote followed by extra switches cannot inject
    /// additional arguments (such as an attacker-controlled <c>--uri</c>) into the desktop tool's argv.
    /// </summary>
    [Fact]
    public async Task ToastTitleCannotInjectCommandLineArguments()
    {
        // This service and the CommandLineToArgvW helper are Windows-only.
        if ( !OperatingSystem.IsWindows() )
        {
            return;
        }

        this.UserDeviceDetection.IsInteractiveDevice = true;

        const string injectedUri = "evil://attacker-controlled";
        const string legitimateUri = "https://metalama.net/legitimate";

        // A title that breaks out of the quoted --title argument and injects its own --uri.
        var maliciousTitle = $"Pwned\" --uri \"{injectedUri}";

        var service = new WindowsUserInterfaceService( this.ServiceProvider );
        service.ShowToastNotification( new ToastNotification( ToastNotificationKinds.News, maliciousTitle, null, legitimateUri ) );

        await this.BackgroundTasks.WhenNoPendingTaskAsync();

        var startInfo = Assert.Single( this.ProcessExecutor.StartedProcesses );
        var args = GetEffectiveArguments( startInfo );

        // The malicious title must reach the child process as a single, verbatim argument.
        Assert.Contains( maliciousTitle, args );

        // The attacker must not be able to inject a second --uri argument.
        var uriPositions = args.Select( ( a, i ) => (Value: a, Index: i) ).Where( t => t.Value == "--uri" ).Select( t => t.Index ).ToList();
        Assert.Single( uriPositions );

        // The single --uri value must be the legitimate URI, never the injected one.
        Assert.Equal( legitimateUri, args[uriPositions[0] + 1] );
        Assert.DoesNotContain( injectedUri, args );
    }

    /// <summary>
    /// Returns the argument vector that the child process would actually receive, independently of whether the
    /// arguments were passed via <see cref="ProcessStartInfo.ArgumentList"/> (the safe form) or via the legacy
    /// <see cref="ProcessStartInfo.Arguments"/> string (which must then be parsed the way Windows would parse it).
    /// </summary>
    private static IReadOnlyList<string> GetEffectiveArguments( ProcessStartInfo startInfo )
        => startInfo.ArgumentList.Count > 0
            ? startInfo.ArgumentList
            : ParseCommandLine( startInfo.Arguments );

    [DllImport( "shell32.dll", SetLastError = true, CharSet = CharSet.Unicode )]
    private static extern IntPtr CommandLineToArgvW( [MarshalAs( UnmanagedType.LPWStr )] string lpCmdLine, out int pNumArgs );

    [DllImport( "kernel32.dll" )]
    private static extern IntPtr LocalFree( IntPtr hMem );

    private static IReadOnlyList<string> ParseCommandLine( string commandLine )
    {
        // CommandLineToArgvW treats argv[0] specially (the program name), so we prepend a dummy token and drop it.
        var argv = CommandLineToArgvW( "dummy.exe " + commandLine, out var argc );

        if ( argv == IntPtr.Zero )
        {
            throw new Win32Exception( Marshal.GetLastWin32Error() );
        }

        try
        {
            var result = new List<string>();

            for ( var i = 1; i < argc; i++ )
            {
                var ptr = Marshal.ReadIntPtr( argv, i * IntPtr.Size );
                result.Add( Marshal.PtrToStringUni( ptr )! );
            }

            return result;
        }
        finally
        {
            LocalFree( argv );
        }
    }
}
