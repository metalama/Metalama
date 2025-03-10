// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using System;
using System.Diagnostics;

namespace Metalama.Backstage.UserInterface;

internal sealed class LinuxUserInterfaceService( IServiceProvider serviceProvider ) : BrowserBasedUserInterfaceService( serviceProvider )
{
    private readonly IProcessExecutor _processExecutor = serviceProvider.GetRequiredBackstageService<IProcessExecutor>();

    protected override ProcessStartInfo GetProcessStartInfoForUrl( string url, BrowserMode browserMode )
    {
        // In some scenarios, like building from Visual Studio Code, starting a process with the URL as the file name doesn't work.
        // We try to use xdg-open to open the URL and if xdg-open is available.
        // xdg-open should be available on most Linux distributions.

        var whichXdgOpen = this._processExecutor.Start( new ProcessStartInfo( "which xdg-open" ) { UseShellExecute = true } );
        whichXdgOpen.WaitForExit();

        if ( whichXdgOpen.ExitCode == 0 )
        {
            // xdg-open is available.
            return new ProcessStartInfo( "xdg-open", url ) { UseShellExecute = true, RedirectStandardOutput = true, RedirectStandardError = true };
        }
        else
        {
            // xdg-open is not available.
            return base.GetProcessStartInfoForUrl( url, browserMode );
        }
    }
}