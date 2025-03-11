// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

internal static class Program
{
    private static bool _canIgnoreRecoverableExceptions = true;

    public static async Task<int> Main( string[] args )
    {
        var serviceCollection = new ServiceCollection();

#pragma warning disable ASP0000
        var serviceProviderBuilder = new ServiceProviderBuilder(
            ( type, instance ) => serviceCollection.Add( new ServiceDescriptor( type, instance, ServiceLifetime.Singleton ) ) );
#pragma warning restore ASP0000

        var initializationOptions =
            new BackstageInitializationOptions( new BackstageWorkerApplicationInfo() )
            {
                AddSupportServices = true, AddLicensing = true, AddUserInterface = true
            };

        serviceProviderBuilder.AddBackstageServices( initializationOptions );

#pragma warning disable ASP0000
        var serviceProvider = serviceCollection.BuildServiceProvider();
#pragma warning restore ASP0000
        _canIgnoreRecoverableExceptions = serviceProvider.GetRequiredBackstageService<IRecoverableExceptionService>().CanIgnore;

        try
        {
            var appData = new AppData( serviceCollection, serviceProvider );
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
            var exceptionReporter = serviceProvider?.GetBackstageService<IExceptionReporter>();

            if ( exceptionReporter != null )
            {
                exceptionReporter.ReportException( e );

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