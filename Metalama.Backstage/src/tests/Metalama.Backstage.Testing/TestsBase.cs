// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Licensing.Registration;
using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Tools;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.Welcome;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using Xunit.Abstractions;
using ILoggerFactory = Metalama.Backstage.Diagnostics.ILoggerFactory;

namespace Metalama.Backstage.Testing
{
    [PublicAPI]
    public abstract class TestsBase
    {
        protected ITestOutputHelper Logger { get; }

        protected TestDateTimeProvider Time { get; } = new();

        protected TestFileSystem FileSystem => this._defaultTestContext.Value.FileSystem;

        protected TestEnvironmentVariableProvider EnvironmentVariableProvider { get; } = new();

        protected TestLoggerFactory Log { get; }

        protected IServiceProvider ServiceProvider => this._defaultTestContext.Value.ServiceProvider;

        // May be null if the different implementation of IConfigurationManager is used.
        protected InMemoryConfigurationManager? ConfigurationManager => this._defaultTestContext.Value.ConfigurationManager;

        protected TestProcessExecutor ProcessExecutor { get; } = new();

        protected TestUserDeviceDetectionService UserDeviceDetection { get; } = new();

        protected TestUserInterfaceService UserInterface => this._defaultTestContext.Value.UserInterface;

        protected BackstageBackgroundTasksService BackgroundTasks { get; } = new();

        protected TestHttpClientFactory HttpClientFactory => this._defaultTestContext.Value.HttpClientFactory;

        protected ILicenseRegistrationService LicenseRegistrationService => this._defaultTestContext.Value.LicenseRegistrationService;

        protected ITelemetryConfigurationService TelemetryConfigurationService => this._defaultTestContext.Value.TelemetryConfigurationService;

        protected virtual LicensingAuthority LicensingAuthority { get; } = LicensingAuthority.GetTestAuthority();

        private TestFileSystem? _uniqueFileSystem;

        protected IServiceCollection CloneServiceCollection()
        {
            var services = new ServiceCollection();

            foreach ( var service in this._defaultTestContext.Value.ServiceCollection )
            {
                services.Add( service );
            }

            return services;
        }

        protected TestsBase( ITestOutputHelper logger, IApplicationInfo? applicationInfo = null )
            : this( logger, new BackstageInitializationOptions( applicationInfo ?? new TestApplicationInfo() ) { AutoUploadTelemetry = false } ) { }

        /// <summary>
        /// Method that can add services. 
        /// </summary>
        protected virtual void ConfigureServices( ServiceProviderBuilder services ) { }

        /// <summary>
        /// Method invoked just after the services are instantiated.
        /// </summary>
        protected virtual void OnAfterServicesCreated( Services services )
        {
            services.ServiceProvider.GetRequiredBackstageService<BackstageServicesInitializer>().Initialize();
        }

        protected void EnsureServicesInitialized()
        {
            _ = this.ServiceProvider;
        }

        internal TestsBase( ITestOutputHelper logger, BackstageInitializationOptions? options )
        {
            this.Logger = logger;

            this.Log = new TestLoggerFactory( logger );

            this._configureServicesAction = this.ConfigureServices;
            this._initializationOptions = options ?? new BackstageInitializationOptions( new TestApplicationInfo() );

            this._defaultTestContext = new Lazy<Services>(
                () =>
                {
                    var services = this.CreateServices( this.ConfigureServicesAction, this.InitializationOptions );
                    this.OnAfterServicesCreated( services );

                    return services;
                } );
        }

        private readonly Lazy<Services> _defaultTestContext;
        private Action<ServiceProviderBuilder> _configureServicesAction;
        private BackstageInitializationOptions _initializationOptions;

        protected Action<ServiceProviderBuilder> ConfigureServicesAction
        {
            get => this._configureServicesAction;
            set
            {
                if ( this._defaultTestContext.IsValueCreated )
                {
                    throw new InvalidOperationException();
                }

                this._configureServicesAction = value;
            }
        }

        protected BackstageInitializationOptions InitializationOptions
        {
            get => this._initializationOptions;
            set
            {
                if ( this._defaultTestContext.IsValueCreated )
                {
                    throw new InvalidOperationException();
                }

                this._initializationOptions = value;
            }
        }

        protected IApplicationInfo ApplicationInfo
        {
            get => this._initializationOptions.ApplicationInfo;
            set
            {
                if ( this._defaultTestContext.IsValueCreated )
                {
                    throw new InvalidOperationException();
                }

                this._initializationOptions = this._initializationOptions with { ApplicationInfo = value };
            }
        }

        protected record Services(
            IServiceCollection ServiceCollection,
            IServiceProvider ServiceProvider,
            InMemoryConfigurationManager? ConfigurationManager,
            TestFileSystem FileSystem,
            TestUserInterfaceService UserInterface,
            TestHttpClientFactory HttpClientFactory,
            ILicenseRegistrationService LicenseRegistrationService,
            ITelemetryConfigurationService TelemetryConfigurationService );

        private Services CreateServices( Action<ServiceProviderBuilder>? serviceBuilder = null, BackstageInitializationOptions? options = null )
        {
            var serviceCollection = this.CreateServiceCollection( serviceBuilder, options );

            var serviceProvider = serviceCollection.BuildServiceProvider();

            return new Services(
                serviceCollection,
                serviceProvider,
                serviceProvider.GetRequiredBackstageService<IConfigurationManager>() as InMemoryConfigurationManager,
                (TestFileSystem) serviceProvider.GetRequiredBackstageService<IFileSystem>(),
                (TestUserInterfaceService) serviceProvider.GetRequiredBackstageService<IUserInterfaceService>(),
                (TestHttpClientFactory) serviceProvider.GetRequiredBackstageService<IHttpClientFactory>(),
                serviceProvider.GetRequiredBackstageService<ILicenseRegistrationService>(),
                serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>() );
        }

        private ServiceCollection CreateServiceCollection(
            Action<ServiceProviderBuilder>? serviceBuilder = null,
            BackstageInitializationOptions? options = null )
        {
            var serviceCollection = new ServiceCollection();
            options ??= new BackstageInitializationOptions( new TestApplicationInfo() );

            serviceCollection
                .AddSingleton( new EarlyLoggerFactory( this.Log ) )
                .AddSingleton<ILoggerFactory>( this.Log )
                .AddSingleton<IApplicationInfoProvider>( new ApplicationInfoProvider( options.ApplicationInfo ) )
                .AddSingleton( new BackstageInitializationOptionsProvider( options ) )
                .AddSingleton<IDateTimeProvider>( this.Time )
                .AddSingleton<IProcessExecutor>( this.ProcessExecutor )
                .AddSingleton<IPlatformInfo>( serviceProvider => new PlatformInfo( serviceProvider, null ) )
                .AddSingleton( this.BackgroundTasks )
                .AddSingleton<IHttpClientFactory>( _ => new TestHttpClientFactory() )
                .AddSingleton<WebLinks>( _ => new WebLinks() )
                .AddSingleton( _ => new RandomNumberGenerator( 0 ) )

                // We must always have a single instance of the file system even if we use CloneServiceCollection.
                .AddSingleton<IFileSystem>( serviceProvider => this._uniqueFileSystem ??= new TestFileSystem( serviceProvider ) )
                .AddSingleton<IEnvironmentVariableProvider>( this.EnvironmentVariableProvider )
                .AddSingleton<IRecoverableExceptionService>( new TestRecoverableExceptionService() )
                .AddSingleton<IUserDeviceDetectionService>( this.UserDeviceDetection )
                .AddSingleton<IConfigurationManager>( serviceProvider => new InMemoryConfigurationManager( serviceProvider ) )
                .AddSingleton<ITempFileManager>( serviceProvider => new TempFileManager( serviceProvider ) )
                .AddSingleton<ILicenseRegistrationService>( serviceProvider => new LicenseRegistrationService( serviceProvider ) )
                .AddSingleton<IBackstageToolsExecutor>( serviceProvider => new BackstageToolsExecutor( serviceProvider ) )
                .AddSingleton<IBackstageToolsLocator>( serviceProvider => new BackstageToolsLocator( serviceProvider ) )
                .AddSingleton<IUserInterfaceService>( serviceProvider => new TestUserInterfaceService( serviceProvider ) )
                .AddSingleton<IToastNotificationService>( serviceProvider => new ToastNotificationService( serviceProvider ) )
                .AddSingleton<IToastNotificationStatusService>( serviceProvider => new ToastNotificationStatusService( serviceProvider ) )
                .AddSingleton<BackstageServicesInitializer>( serviceProvider => new BackstageServicesInitializer( serviceProvider, options ) )
                .AddSingleton<WelcomeService>( serviceProvider => new WelcomeService( serviceProvider ) )
                .AddSingleton<IIdeExtensionStatusService>( serviceProvider => new IdeExtensionStatusService( serviceProvider ) )
                .AddSingleton<IToastNotificationDetectionService>( serviceProvider => new ToastNotificationDetectionService( serviceProvider ) )
                .AddSingleton<IStandardDirectories>( serviceProvider => new StandardDirectories( serviceProvider ) )
                .AddSingleton<IBackstageToolsExtractor>( serviceProvider => new BackstageToolsExtractor( serviceProvider ) )
                .AddSingleton<ITelemetryConfigurationService>( serviceProvider => new TelemetryConfigurationService( serviceProvider, Guid.Empty ) )
                .AddSingleton( this.LicensingAuthority );

            var serviceProviderBuilder =
                new ServiceProviderBuilder( ( type, instance ) => serviceCollection.AddSingleton( type, instance ) );

            // The test implementation may replace some services.
            serviceBuilder?.Invoke( serviceProviderBuilder );

            return serviceCollection;
        }
    }
}