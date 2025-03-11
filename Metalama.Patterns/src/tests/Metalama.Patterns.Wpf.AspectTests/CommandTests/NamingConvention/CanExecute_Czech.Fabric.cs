// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Fabrics;
using Metalama.Patterns.Wpf.Configuration;

namespace Doc.Command.CanExecute_Czech;

public class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.ConfigureCommand(
            builder =>
            {
                builder.AddNamingConvention(
                    new CommandNamingConvention( "czech-1" )
                    {
                        CommandNamePattern = "^Vykonat(?<CommandName>.*)$",
                        CanExecutePatterns = ["^MůžemeVykonat{CommandName}$"],
                        CommandPropertyName = "{CommandName}Příkaz"
                    } );

                builder.AddNamingConvention(
                    new CommandNamingConvention( "czech-2" ) { CanExecutePatterns = ["^Můžeme{CommandName}$"], CommandPropertyName = "{CommandName}Příkaz" } );
            } );
    }
}