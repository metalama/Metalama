// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.Commands.Configuration;

internal class PrintConfigurationCommand : BaseConfigurationCommand
{
    protected override void Execute( ExtendedCommandContext context, ConfigurationFileCommandAdapter adapter ) => adapter.Print( context );
}