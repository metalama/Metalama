// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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