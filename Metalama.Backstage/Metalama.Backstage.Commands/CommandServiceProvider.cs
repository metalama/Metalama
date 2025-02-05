// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Metalama.Backstage.Commands
{
    internal class CommandServiceProvider : ICommandServiceProviderProvider
    {
        private readonly IApplicationInfo _applicationInfo;

        public CommandServiceProvider( IApplicationInfo applicationInfo )
        {
            this._applicationInfo = applicationInfo;
        }

        public IServiceProvider GetServiceProvider( CommandServiceProviderArgs args )
        {
            // ReSharper disable RedundantTypeArgumentsOfMethod

            var serviceCollection = new ServiceCollection();

            var serviceProviderBuilder = new ServiceProviderBuilder(
                ( type, func ) => serviceCollection.Add( new ServiceDescriptor( type, func, ServiceLifetime.Singleton ) ) );

            serviceProviderBuilder.AddService( typeof(ILoggerFactory), new AnsiConsoleLoggerFactory( args.Console, args.Settings ) );
            var loggerFactory = serviceCollection.BuildServiceProvider().GetLoggerFactory();

            var initializationOptions = new BackstageInitializationOptions( this._applicationInfo )
            {
                AddLicensing = true,
                AddSupportServices = true,
                CreateLoggingFactory = _ => loggerFactory,
                IsDevelopmentEnvironment = args.Settings.IsDevelopmentEnvironment,
                AddUserInterface = args.Settings.AddUserInterface,
                AddToolsExtractor = b => b.AddTools()
            };

            initializationOptions = args.TransformOptions( initializationOptions );

            serviceProviderBuilder.AddBackstageServices( initializationOptions );

            return serviceCollection.BuildServiceProvider();
        }
    }
}