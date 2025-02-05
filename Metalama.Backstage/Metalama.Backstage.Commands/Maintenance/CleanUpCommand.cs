// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Maintenance;

namespace Metalama.Backstage.Commands.Maintenance;

internal class CleanUpCommand : BaseCommand<CleanUpCommandSettings>
{
    protected override void Execute( ExtendedCommandContext context, CleanUpCommandSettings settings )
    {
        if ( settings is { All: true, DoNotKill: false } )
        {
            context.Console.WriteHeading( "Killing Metalama processes" );

            // Automatically kill processes before Cleanup unless --no-kill option is used.
            var processManager = context.ServiceProvider.GetRequiredBackstageService<IProcessManager>();
            processManager.KillCompilerProcesses( true );
        }

        context.Console.WriteHeading( "Cleaning up temporary files. " );

        var tempFileManager = new TempFileManager( context.ServiceProvider );

        tempFileManager.CleanTempDirectories( true, settings.All );

        if ( settings.All )
        {
            context.Console.WriteSuccess( "Temporary files have been cleaned up." );
        }
        else
        {
            context.Console.WriteSuccess( "Unused temporary files have been cleaned up." );
        }
    }
}