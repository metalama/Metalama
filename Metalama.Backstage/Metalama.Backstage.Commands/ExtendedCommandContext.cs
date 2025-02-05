// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using Spectre.Console.Cli;
using System;

namespace Metalama.Backstage.Commands;

[PublicAPI]
public class ExtendedCommandContext
{
    public ConsoleWriter Console { get; }

    public IServiceProvider ServiceProvider { get; }

    public BackstageCommandOptions BackstageCommandOptions { get; }

    internal ExtendedCommandContext(
        CommandContext commandContext,
        BaseCommandSettings settings,
        Func<BackstageInitializationOptions, BackstageInitializationOptions> transformOptions )
    {
        this.BackstageCommandOptions = (BackstageCommandOptions) commandContext.Data!;
        this.Console = new ConsoleWriter( this.BackstageCommandOptions );

        this.ServiceProvider =
            this.BackstageCommandOptions.ServiceProvider.GetServiceProvider( new CommandServiceProviderArgs( this.Console, settings, transformOptions ) );
    }

    protected ExtendedCommandContext( ExtendedCommandContext prototype )
    {
        this.Console = prototype.Console;
        this.ServiceProvider = prototype.ServiceProvider;
        this.BackstageCommandOptions = prototype.BackstageCommandOptions;
    }
}