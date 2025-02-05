// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Commands
{
    /// <summary>
    /// Creates an instance <see cref="IServiceProvider"/> configured for a console command.
    /// </summary>
    internal interface ICommandServiceProviderProvider
    {
        IServiceProvider GetServiceProvider( CommandServiceProviderArgs args );
    }

    internal record CommandServiceProviderArgs(
        ConsoleWriter Console,
        BaseCommandSettings Settings,
        Func<BackstageInitializationOptions, BackstageInitializationOptions> TransformOptions );
}