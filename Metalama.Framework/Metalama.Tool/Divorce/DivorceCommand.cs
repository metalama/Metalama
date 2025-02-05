// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Commands;
using System.IO;

namespace Metalama.Tool.Divorce;

[UsedImplicitly]
internal class DivorceCommand : BaseCommand<DivorceCommandSettings>
{
    protected override void Execute( ExtendedCommandContext context, DivorceCommandSettings settings )
    {
        context.Console.WriteHeading( "Performing divorce" );

        var divorceService = new DivorceService( context.ServiceProvider, Directory.GetCurrentDirectory() );

        if ( !settings.Force )
        {
            divorceService.CheckGitStatus();
        }

        divorceService.PerformDivorce();

        context.Console.WriteSuccess( $"Divorce feature performed successfully." );
    }
}