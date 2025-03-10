// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Audit;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Licensing.Registration;
using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Telemetry.User;
using Metalama.Backstage.Tools;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.Utilities;
using Metalama.Backstage.Welcome;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;

namespace Metalama.Backstage.Extensibility;

/// <summary>
/// Extension methods for setting up default services in an <see cref="ServiceProviderBuilder" />.
/// </summary>
public static class RegisterServiceExtensions
{
    internal static ServiceProviderBuilder AddSingleton<T>(
        this ServiceProviderBuilder serviceProviderBuilder,
        T instance )
        where T : IBackstageService
    {
        serviceProviderBuilder.AddService( typeof(T), instance );

        return serviceProviderBuilder;
    }

    internal static ServiceProviderBuilder AddSingleton<T>(
        this ServiceProviderBuilder serviceProviderBuilder,
        Func<IServiceProvider, T> func )
        where T : IBackstageService
    {
        serviceProviderBuilder.AddService( typeof(T), serviceProvider => func( serviceProvider ) );

        return serviceProviderBuilder;
    }

    internal static ServiceProviderBuilder AddDiagnostics(
        this ServiceProviderBuilder serviceProviderBuilder,
        ProcessKind processKind )
    {
        serviceProviderBuilder.AddSingleton<ILoggerFactory>(
            serviceProvider =>
            {
                var dateTimeProvider = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();

                var configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
                var configuration = configurationManager.Get<DiagnosticsConfiguration>();

                DebuggerHelper.Launch( configuration, processKind );

                var consoleTracing = Environment.GetEnvironmentVariable( "METALAMA_CONSOLE_TRACE" );

                ILoggerFactory loggerFactory;

                if ( !string.IsNullOrWhiteSpace( consoleTracing ) )
                {
                    var traceCategories = consoleTracing.Split( ' ', ',', ';' ).ToImmutableHashSet();
                    loggerFactory = new ConsoleLoggerFactory( Console.Out, traceCategories );
                }
                else
                {
                    // Automatically stop logging after a while.
                    var lastAcceptableModificationTime = dateTimeProvider.UtcNow.AddHours( -configuration.Logging.StopLoggingAfterHours );

                    if ( configuration.Timestamp != null && configuration.Timestamp.Value.ToUtcDateTime() < lastAcceptableModificationTime )
                    {
                        configurationManager.UpdateIf<DiagnosticsConfiguration>(
                            c => c.Logging.Processes.Any( p => p.Value ),
                            c => c with { Logging = c.Logging with { Processes = c.Logging.Processes.ToImmutableDictionary( x => x.Key, _ => false ) } } );

                        configuration = configurationManager.Get<DiagnosticsConfiguration>();
                    }

                    var applicationInfo = serviceProvider.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication;

                    loggerFactory = new LoggerFactory(
                        serviceProvider,
                        configuration,
                        applicationInfo.ProcessKind );
                }

                serviceProvider.GetBackstageService<EarlyLoggerFactory>()?.Replace( loggerFactory );

                return loggerFactory;
            } );

        serviceProviderBuilder.AddSingleton<IProfilingService>( serviceProvider => new ProfilingService( serviceProvider ) );

        return serviceProviderBuilder;
    }

    /// <summary>
    /// Adds the minimal set of services required by logging and telemetry.
    /// </summary>
    private static void AddCommonServices(
        this ServiceProviderBuilder serviceProviderBuilder,
        IApplicationInfo applicationInfo,
        BackstageInitializationOptions options )
        => _ = serviceProviderBuilder
            .AddSingleton( _ => new BackstageInitializationOptionsProvider( options ) )
            .AddSingleton( _ => new EarlyLoggerFactory() )
            .AddSingleton( _ => new RandomNumberGenerator() )
            .AddSingleton<IEnvironmentVariableProvider>( new EnvironmentVariableProvider() )
            .AddSingleton<IRecoverableExceptionService>( serviceProvider => new RecoverableExceptionService( serviceProvider ) )
            .AddSingleton<IApplicationInfoProvider>( new ApplicationInfoProvider( applicationInfo ) )
            .AddSingleton<IUserDeviceDetectionService>( serviceProvider => new WindowsUserDeviceDetectionService( serviceProvider ) )
            .AddSingleton<IDateTimeProvider>( new CurrentDateTimeProvider() )
            .AddSingleton<IFileSystem>( new FileSystem() )
            .AddSingleton<IStandardDirectories>( serviceProvider => new StandardDirectories( serviceProvider ) )
            .AddSingleton<IProcessExecutor>( new ProcessExecutor() )
            .AddSingleton<IHttpClientFactory>( new HttpClientFactory() )
            .AddSingleton<IConfigurationManager>( serviceProvider => new ConfigurationManager( serviceProvider ) )
            .AddSingleton<IPlatformInfo>( serviceProvider => new PlatformInfo( serviceProvider, options.DotNetSdkDirectory ) )
            .AddSingleton<BackstageBackgroundTasksService>( _ => BackstageBackgroundTasksService.Default )
            .AddSingleton<WebLinks>( _ => new WebLinks() )
            .AddSingleton<ITempFileManager>( serviceProvider => new TempFileManager( serviceProvider ) )
            .AddSingleton( serviceProvider => new ShutdownService( serviceProvider ) );

    private static void AddLicensing(
        this ServiceProviderBuilder serviceProviderBuilder,
        LicensingInitializationOptions options,
        IApplicationInfo applicationInfo )
    {
        var authority = options.UseTestAuthority ? LicensingAuthority.GetTestAuthority() : LicensingAuthority.GetProductionAuthority();
        serviceProviderBuilder.AddSingleton( authority );

        if ( applicationInfo.IsLicenseAuditEnabled )
        {
            serviceProviderBuilder.AddSingleton<ILicenseAuditManager>( serviceProvider => new LicenseAuditManager( serviceProvider ) );
        }

        serviceProviderBuilder.AddSingleton( serviceProvider => LicenseConsumptionServiceFactory.Create( serviceProvider, options ) );
        serviceProviderBuilder.AddSingleton<ILicenseRegistrationService>( serviceProvider => new LicenseRegistrationService( serviceProvider ) );
    }

    public static ServiceProviderBuilder AddBackstageServices( this ServiceProviderBuilder serviceProviderBuilder, BackstageInitializationOptions options )
    {
        // Add base services.
        var applicationInfo = options.ApplicationInfo;

        serviceProviderBuilder.AddCommonServices( applicationInfo, options );

        // Add diagnostics.
        if ( options.AddSupportServices )
        {
            if ( options.CreateLoggingFactory == null )
            {
                serviceProviderBuilder.AddDiagnostics( applicationInfo.ProcessKind );
            }
            else
            {
                serviceProviderBuilder.AddSingleton<ILoggerFactory>(
                    serviceProvider =>
                    {
                        var loggerFactory = options.CreateLoggingFactory( serviceProvider );
                        serviceProvider.GetBackstageService<EarlyLoggerFactory>()?.Replace( loggerFactory );

                        return loggerFactory;
                    } );
            }
        }

        // Add file locking detection.
        if ( LockingProcessDetector.IsSupported )
        {
            serviceProviderBuilder.AddService( typeof(ILockingProcessDetector), _ => new LockingProcessDetector() );
        }

        if ( options.AddDumperService || options.AddSupportServices )
        {
            serviceProviderBuilder.AddService( typeof(IMiniDumper), serviceProvider => new MiniDumper( serviceProvider ) );
        }

        // Add support services.
        if ( options.AddSupportServices )
        {
            serviceProviderBuilder.AddTelemetryServices();
        }

        // Add tools.
        if ( options.AddSupportServices || options.AddUserInterface )
        {
            if ( options.IsDevelopmentEnvironment )
            {
                serviceProviderBuilder.AddService( typeof(IBackstageToolsLocator), _ => new DevBackstageToolsLocator() );
            }
            else
            {
                serviceProviderBuilder.AddService( typeof(IBackstageToolsLocator), serviceProvider => new BackstageToolsLocator( serviceProvider ) );
            }

            serviceProviderBuilder.AddService( typeof(IBackstageToolsExecutor), serviceProvider => new BackstageToolsExecutor( serviceProvider ) );
            options.AddToolsExtractor?.Invoke( serviceProviderBuilder );
        }

        // Add user interface.
        if ( options.AddUserInterface )
        {
            serviceProviderBuilder.AddSingleton( serviceProvider => new WelcomeService( serviceProvider ) );

            serviceProviderBuilder.AddService(
                typeof(IToastNotificationStatusService),
                serviceProvider => new ToastNotificationStatusService( serviceProvider ) );

            serviceProviderBuilder.AddService( typeof(IToastNotificationService), serviceProvider => new ToastNotificationService( serviceProvider ) );

            if ( options.DetectToastNotifications )
            {
                serviceProviderBuilder.AddService(
                    typeof(IToastNotificationDetectionService),
                    serviceProvider => new ToastNotificationDetectionService( serviceProvider ) );
            }

            if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
            {
                serviceProviderBuilder.AddService( typeof(IIdeExtensionStatusService), serviceProvider => new IdeExtensionStatusService( serviceProvider ) );
                serviceProviderBuilder.AddService( typeof(IUserInterfaceService), serviceProvider => new WindowsUserInterfaceService( serviceProvider ) );
            }
            else if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
            {
                serviceProviderBuilder.AddService( typeof(IUserInterfaceService), serviceProvider => new LinuxUserInterfaceService( serviceProvider ) );
            }
            else
            {
                serviceProviderBuilder.AddService( typeof(IUserInterfaceService), serviceProvider => new BrowserBasedUserInterfaceService( serviceProvider ) );
            }

            serviceProviderBuilder.AddService( typeof(IUserInfoService), serviceProvider => new UserInfoService( serviceProvider ) );
        }

        // Add process management service.
        serviceProviderBuilder.TryAddProcessManagerService();

        // Add licensing.
        if ( options.AddLicensing )
        {
            if ( applicationInfo.IsLicenseAuditEnabled && !options.AddSupportServices )
            {
                throw new InvalidOperationException( "License audit requires support services." );
            }

            serviceProviderBuilder.AddLicensing( options.LicensingOptions, applicationInfo );
        }

        // Add initialization services.
        serviceProviderBuilder.AddSingleton( serviceProvider => new BackstageServicesInitializer( serviceProvider ) );

        return serviceProviderBuilder;
    }

    internal static void AddTelemetryServices( this ServiceProviderBuilder serviceProviderBuilder )
    {
        // Add telemetry.
        serviceProviderBuilder
            .AddSingleton( serviceProvider => new TelemetryLogger( serviceProvider ) )
            .AddSingleton<LocalExceptionReporter>( serviceProvider => new LocalExceptionReporter( serviceProvider ) )
            .AddSingleton<IExceptionReporter>( serviceProvider => new ExceptionReporter( new TelemetryQueue( serviceProvider ), serviceProvider ) )
            .AddSingleton<ITelemetryUploader>( serviceProvider => new TelemetryUploader( serviceProvider ) )
            .AddSingleton<IUsageReporter>( serviceProvider => new UsageReporter( serviceProvider ) )
            .AddSingleton<ITelemetryConfigurationService>( serviceProvider => new TelemetryConfigurationService( serviceProvider ) )
            .AddSingleton<TelemetryReportUploader>( serviceProvider => new TelemetryReportUploader( serviceProvider ) )
            .AddSingleton<MatomoAuditUploader>( serviceProvider => new MatomoAuditUploader( serviceProvider ) );
    }

    private static void TryAddProcessManagerService( this ServiceProviderBuilder serviceProviderBuilder )
    {
        if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            serviceProviderBuilder.AddSingleton<IProcessManager>( serviceProvider => new WindowsProcessManager( serviceProvider ) );
        }
        else if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
        {
            serviceProviderBuilder.AddSingleton<IProcessManager>( serviceProvider => new LinuxProcessManager( serviceProvider ) );
        }
        else if ( RuntimeInformation.IsOSPlatform( OSPlatform.OSX ) )
        {
            serviceProviderBuilder.AddSingleton<IProcessManager>( serviceProvider => new MacProcessManager( serviceProvider ) );
        }
        else
        {
            // Not supported.
        }
    }
}