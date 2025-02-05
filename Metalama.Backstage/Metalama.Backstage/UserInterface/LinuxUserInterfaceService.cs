// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using System;
using System.Diagnostics;

namespace Metalama.Backstage.UserInterface;

internal class LinuxUserInterfaceService( IServiceProvider serviceProvider ) : BrowserBasedUserInterfaceService( serviceProvider )
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