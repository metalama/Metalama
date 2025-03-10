// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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