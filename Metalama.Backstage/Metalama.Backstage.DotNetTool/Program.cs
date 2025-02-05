// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Commands;
using Spectre.Console.Cli;

namespace Metalama.Backstage.DotNetTool;

internal static class Program
{
    public static int Main( string[] args )
    {
        var app = new CommandApp();

        var options = new BackstageCommandOptions( new ApplicationInfo() );

        BackstageCommandFactory.ConfigureCommandApp(
            app,
            options,
            builder => builder.AddCommand<ThrowCommand>( "throw" ).WithData( options ) );

        return app.Run( args );
    }
}