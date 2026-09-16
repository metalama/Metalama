// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Backstage.Desktop.Windows;

/// <summary>
/// The options of <see cref="BackstageDesktopProgram.Run"/>, which a product supplies from its executable.
/// </summary>
/// <param name="CreateBackstageServices">Creates the Backstage services of the product. The parameter indicates whether the <c>--dev</c> option was given, in which case the services locate the worker in the build output of the repository. The notifier needs the support services and the user interface services, without the detection of the toast notifications.</param>
[PublicAPI]
public sealed record BackstageDesktopOptions( Func<bool, IServiceProvider> CreateBackstageServices );

/// <summary>
/// The entry point of the desktop notifier, which the executable of a product calls from its <c>Main</c> method. The
/// notifier shows the toast notifications (<c>notify</c> command), reacts to their buttons (<c>snooze</c>, <c>mute</c>,
/// <c>setup</c>, <c>rss</c>, <c>privacy</c>, <c>exception-report</c>, <c>report-exception</c> commands), and stays
/// registered with Windows so that a toast activates it after it has exited.
/// </summary>
[PublicAPI]
public static class BackstageDesktopProgram
{
    /// <summary>
    /// Runs the notifier with the given command line. The calling thread must be a single-threaded apartment
    /// thread, which the <c>Main</c> method of the executable obtains with <see cref="STAThreadAttribute"/>.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <param name="options">The options that bind the notifier to a product.</param>
    /// <returns>The exit code of the process.</returns>
    public static int Run( string[] args, BackstageDesktopOptions options )
    {
        DesktopServices.Initialize( options );

        var application = new DesktopApplication( args );

        return application.Run();
    }
}
