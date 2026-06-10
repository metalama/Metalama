// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Testing;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.UserInterface.Toasts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    private const string _injectedUri = "evil://attacker-controlled";
    private const string _legitimateUri = "https://metalama.net/legitimate";

    // This service and the CommandLineToArgvW helper are Windows-only. Fully qualified to avoid binding to the
    // TestRuntimeInformation member of the base class.
    private static bool IsWindows => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform( OSPlatform.Windows );

    public WindowsUserInterfaceServiceTests( ITestOutputHelper logger ) : base( logger ) { }

    public static IEnumerable<object[]> MaliciousTitles()
    {
        // Break out of the quoted argument and inject a second --uri.
        yield return ["Pwned\" --uri \"" + _injectedUri];

        // A trailing backslash before the closing quote must not let the quote be escaped away.
        yield return ["Pwned\\\" --uri \"" + _injectedUri];

        // Multiple backslashes followed by a quote.
        yield return ["Pwned\\\\\" --uri \"" + _injectedUri];
    }

    /// <summary>
    /// Verifies that a toast notification whose title contains quotes/backslashes followed by extra switches cannot
    /// inject additional arguments (such as an attacker-controlled <c>--uri</c>) into the desktop tool's argv.
    /// </summary>
    [Theory]
    [MemberData( nameof(MaliciousTitles) )]
    public async Task ToastTitleCannotInjectCommandLineArguments( string maliciousTitle )
    {
        if ( !IsWindows )
        {
            return;
        }

        var args = await this.ShowNotificationAndGetArgumentsAsync( new ToastNotification( ToastNotificationKinds.News, maliciousTitle, null, _legitimateUri ) );

        // The malicious title must reach the child process as a single, verbatim argument.
        AssertSwitchValue( args, "--title", maliciousTitle );

        // The attacker must not be able to inject a second --uri argument: exactly one --uri, with the legitimate value.
        AssertSwitchValue( args, "--uri", _legitimateUri );
        Assert.DoesNotContain( _injectedUri, args );
    }

    /// <summary>
    /// Verifies that the notification text (also potentially untrusted) cannot inject additional arguments.
    /// </summary>
    [Fact]
    public async Task ToastTextCannotInjectCommandLineArguments()
    {
        if ( !IsWindows )
        {
            return;
        }

        var maliciousText = "Body\" --uri \"" + _injectedUri;

        var args = await this.ShowNotificationAndGetArgumentsAsync(
            new ToastNotification( ToastNotificationKinds.News, "Title", maliciousText, _legitimateUri ) );

        AssertSwitchValue( args, "--text", maliciousText );
        AssertSwitchValue( args, "--uri", _legitimateUri );
        Assert.DoesNotContain( _injectedUri, args );
    }

    /// <summary>
    /// Verifies that a benign notification still passes its title and URI through to the child process unchanged.
    /// </summary>
    [Fact]
    public async Task BenignToastNotificationPassesArgumentsCorrectly()
    {
        if ( !IsWindows )
        {
            return;
        }

        const string title = "Metalama 2026.1 released";

        var args = await this.ShowNotificationAndGetArgumentsAsync(
            new ToastNotification( ToastNotificationKinds.News, title, null, _legitimateUri ) );

        Assert.Contains( "notify", args );
        AssertSwitchValue( args, "--title", title );
        AssertSwitchValue( args, "--uri", _legitimateUri );
    }

    private async Task<IReadOnlyList<string>> ShowNotificationAndGetArgumentsAsync( ToastNotification notification )
    {
        this.UserDeviceDetection.IsInteractiveDevice = true;

        var service = new WindowsUserInterfaceService( this.ServiceProvider );
        service.ShowToastNotification( notification );

        await this.BackgroundTasks.WhenNoPendingTaskAsync();

        var startInfo = Assert.Single( this.ProcessExecutor.StartedProcesses );

        // The executor builds a single, escaped command-line string (ProcessStartInfo.ArgumentList is not available
        // on all target frameworks). We re-parse it the way Windows would to recover the effective argument vector.
        return ParseCommandLine( startInfo.Arguments );
    }

    /// <summary>
    /// Asserts that <paramref name="switchName"/> appears exactly once in <paramref name="args"/> and that the token
    /// immediately following it equals <paramref name="expectedValue"/>.
    /// </summary>
    private static void AssertSwitchValue( IReadOnlyList<string> args, string switchName, string expectedValue )
    {
        var positions = args.Select( ( a, i ) => (Value: a, Index: i) ).Where( t => t.Value == switchName ).Select( t => t.Index ).ToList();
        Assert.Single( positions );
        Assert.True( positions[0] + 1 < args.Count, $"'{switchName}' has no value." );
        Assert.Equal( expectedValue, args[positions[0] + 1] );
    }

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
