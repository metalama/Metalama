// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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