// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Worker.Upload;
using Metalama.Backstage.Worker.WebServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spectre.Console.Cli;
using System;
using System.Threading.Tasks;

namespace Metalama.Backstage.Worker;

/// <summary>
/// The options of <see cref="BackstageWorkerProgram.RunAsync"/>, which a product supplies from its executable.
/// </summary>
/// <param name="AddBackstageServices">Registers the Backstage services of the product. The worker needs the support services, the licensing services and the user interface services.</param>
/// <param name="InitializeBackstageServices">Initializes the Backstage services of a container after it is built. It is invoked for the container of the process and for the container of the web server.</param>
[PublicAPI]
public sealed record BackstageWorkerOptions(
    Action<ServiceProviderBuilder> AddBackstageServices,
    Action<IServiceProvider> InitializeBackstageServices );

/// <summary>
/// The entry point of the worker application, which the executable of a product calls from its <c>Main</c> method.
/// The worker hosts the setup web server (<c>web</c> command) and uploads the telemetry (<c>upload</c> command).
/// </summary>
[PublicAPI]
public static class BackstageWorkerProgram
{
    private static bool _canIgnoreRecoverableExceptions = true;

    /// <summary>
    /// Runs the worker with the given command line.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <param name="options">The options that bind the worker to a product.</param>
    /// <returns>The exit code of the process.</returns>
    public static async Task<int> RunAsync( string[] args, BackstageWorkerOptions options )
    {
        var serviceCollection = new ServiceCollection();

#pragma warning disable ASP0000
        var serviceProviderBuilder = new ServiceProviderBuilder(
            ( type, instance ) => serviceCollection.Add( new ServiceDescriptor( type, instance, ServiceLifetime.Singleton ) ) );
#pragma warning restore ASP0000

        options.AddBackstageServices( serviceProviderBuilder );

#pragma warning disable ASP0000
        var serviceProvider = serviceCollection.BuildServiceProvider();
#pragma warning restore ASP0000
        options.InitializeBackstageServices( serviceProvider );
        _canIgnoreRecoverableExceptions = serviceProvider.GetRequiredBackstageService<IRecoverableExceptionService>().CanIgnore;

        try
        {
            var appData = new AppData( serviceCollection, serviceProvider, options.InitializeBackstageServices );
            var app = new CommandApp();

            app.Configure(
                configuration =>
                {
                    configuration.PropagateExceptions();
                    configuration.AddCommand<UploadCommand>( "upload" ).WithData( appData );
                    configuration.AddCommand<WebServerCommand>( "web" ).WithData( appData );
                } );

            return await app.RunAsync( args );
        }
        catch ( Exception e )
        {
            if ( !HandleException( serviceProvider, e ) )
            {
                throw;
            }

            if ( !_canIgnoreRecoverableExceptions )
            {
                throw;
            }

            return -1;
        }
    }

    private static bool HandleException( IServiceProvider? serviceProvider, Exception e )
    {
        try
        {
            // A worker crash is telemetry about the tooling itself: report through the tooling policy. See #1701.
            if ( serviceProvider != null )
            {
                serviceProvider.ReportToolingException( e );

                return true;
            }
        }
        catch when ( _canIgnoreRecoverableExceptions )
        {
            // We don't want failing telemetry to disturb users.
        }

        try
        {
            var log = serviceProvider?.GetLoggerFactory().GetLogger( "BackstageWorker" ).Error;

            if ( log != null )
            {
                log.Log( $"Unhandled exception: {e}" );

                return true;
            }
        }
        catch when ( _canIgnoreRecoverableExceptions )
        {
            // We don't want failing telemetry to disturb users.
        }

        return false;
    }
}
