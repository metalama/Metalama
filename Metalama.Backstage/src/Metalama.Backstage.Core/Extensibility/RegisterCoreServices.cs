// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Serialization;
using Metalama.Backstage.Threading;
using Metalama.Backstage.Tools;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.Utilities;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;

namespace Metalama.Backstage.Extensibility;

/// <summary>
/// Extension methods that register the core services in a <see cref="ServiceProviderBuilder"/>.
/// </summary>
public static class RegisterCoreServices
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

    /// <summary>
    /// Registers the core services: the product profile, the application information, the event dispatcher, the
    /// infrastructure services, the temporary file manager, and optionally the diagnostics, the crash dump service and
    /// the tool services. The configuration manager is not registered here; a host registers the one of the
    /// configuration package, or an in-memory one.
    /// </summary>
    public static ServiceProviderBuilder AddCoreServices( this ServiceProviderBuilder serviceProviderBuilder, CoreInitializationOptions options )
    {
        var applicationInfo = options.ApplicationInfo;

        serviceProviderBuilder
            .AddSingleton( options.ProductProfile )
            .AddSingleton( _ => new EarlyLoggerFactory() )
            .AddSingleton<IEventDispatcher>( serviceProvider => new EventDispatcher( serviceProvider ) )
            .AddSingleton( _ => new RandomNumberGenerator() )
            .AddSingleton<IEnvironmentVariableProvider>( _ => new EnvironmentVariableProvider() )
            .AddSingleton<IRuntimeInformation>( _ => new RuntimeInformationProvider() )
            .AddSingleton<IMachineIdProvider>( CreateMachineIdProvider )
            .AddSingleton<IRecoverableExceptionService>( serviceProvider => new RecoverableExceptionService( serviceProvider ) )
            .AddSingleton<IApplicationInfoProvider>( new ApplicationInfoProvider( applicationInfo ) )
            .AddSingleton<IUserDeviceDetectionService>( serviceProvider => new WindowsUserDeviceDetectionService( serviceProvider ) )
            .AddSingleton<IDateTimeProvider>( _ => new CurrentDateTimeProvider() )
            .AddSingleton<IFileSystem>( serviceProvider => new FileSystem( serviceProvider ) )
            .AddSingleton<IStandardDirectories>( serviceProvider => new StandardDirectories( serviceProvider ) )
            .AddSingleton<IProcessExecutor>( _ => new ProcessExecutor() )
            .AddSingleton<IHttpClientFactory>( _ => new HttpClientFactory() )
            .AddSingleton<IJsonSerializationService>( _ => new JsonSerializationService( options.JsonTypeInfoResolvers ) )
            .AddSingleton<INamedLockService>( CreateNamedLockService )
            .AddSingleton<IPlatformInfo>( serviceProvider => new PlatformInfo( serviceProvider ) )
            .AddSingleton<BackstageBackgroundTasksService>( _ => new BackstageBackgroundTasksService() )
            .AddSingleton<ITempFileManager>( serviceProvider => new TempFileManager( serviceProvider ) )
            .AddSingleton( serviceProvider => new ShutdownService( serviceProvider ) );

        if ( options.AddDiagnostics )
        {
            if ( options.DiagnosticsOptions.CreateLoggingFactory == null )
            {
                serviceProviderBuilder.AddDiagnostics( applicationInfo.ProcessKind, options.DiagnosticsOptions );
            }
            else
            {
                serviceProviderBuilder.AddSingleton<ILoggerFactory>(
                    serviceProvider =>
                    {
                        var loggerFactory = options.DiagnosticsOptions.CreateLoggingFactory( serviceProvider );
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

        if ( options.AddDumper || options.AddDiagnostics )
        {
            serviceProviderBuilder.AddService( typeof(IMiniDumper), serviceProvider => new MiniDumper( serviceProvider ) );
        }

        if ( options.AddTools )
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

        serviceProviderBuilder.TryAddProcessManagerService();

        return serviceProviderBuilder;
    }

    internal static void AddDiagnostics(
        this ServiceProviderBuilder serviceProviderBuilder,
        ProcessKind processKind,
        DiagnosticsInitializationOptions options )
    {
        serviceProviderBuilder.AddSingleton<ILoggerFactory>(
            serviceProvider =>
            {
                var dateTimeProvider = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();

                var configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
                var configuration = configurationManager.Get<DiagnosticsConfiguration>();

                DebuggerHelper.Launch( configuration, processKind );

                var productProfile = serviceProvider.GetRequiredBackstageService<ProductProfile>();
                var consoleTracing = Environment.GetEnvironmentVariable( productProfile.GetEnvironmentVariableName( "CONSOLE_TRACE" ) );

                ILoggerFactory loggerFactory;

                if ( !string.IsNullOrWhiteSpace( consoleTracing ) )
                {
                    var traceCategories = consoleTracing.Split( ' ', ',', ';' ).ToImmutableHashSet();
                    loggerFactory = new ConsoleLoggerFactory( productProfile.Name, Console.Out, traceCategories );
                }
                else if ( options.TraceAction != null )
                {
                    loggerFactory = new DelegateLoggerFactory( productProfile.Name, options.TraceAction, ImmutableHashSet.Create( "*" ) );
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
    }

    /// <summary>
    /// Creates the implementation of <see cref="IMachineIdProvider"/> that reads the identifier of the machine on the
    /// current operating system.
    /// </summary>
    private static IMachineIdProvider CreateMachineIdProvider( IServiceProvider serviceProvider )
    {
        if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            return new WindowsMachineIdProvider( serviceProvider );
        }
        else if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
        {
            return new LinuxMachineIdProvider( serviceProvider );
        }
        else if ( RuntimeInformation.IsOSPlatform( OSPlatform.OSX ) )
        {
            return new MacMachineIdProvider( serviceProvider );
        }
        else
        {
            return new MachineNameMachineIdProvider( serviceProvider );
        }
    }

    /// <summary>
    /// Creates the named lock service and routes its events to the log.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The logger comes from <see cref="EarlyLoggerFactory"/>, which buffers until the real logging services are
    /// available. That is necessary because <see cref="IConfigurationManager"/> is a consumer of this service and
    /// is itself a dependency of the logging services, so resolving a real logger here would be a cycle.
    /// </para>
    /// <para>
    /// The filter keeps the routine acquisitions and releases from being reported at all unless tracing is on.
    /// Without it, subscribing would cost one event object per acquisition on the critical path of every
    /// compilation, only for the logger to discard it.
    /// </para>
    /// </remarks>
    private static INamedLockService CreateNamedLockService( IServiceProvider serviceProvider )
    {
        var service = new NamedLockService( serviceProvider );
        var logger = serviceProvider.GetRequiredBackstageService<EarlyLoggerFactory>().GetLogger( "NamedLock" );

        service.ReportFilter = kind => logger.Trace != null || LockEventArgs.IsWarningKind( kind );

        service.LockEventReported += ( _, lockEvent ) =>
        {
            if ( lockEvent.IsWarning )
            {
                logger.Warning?.Log( lockEvent.ToString() );
            }
            else
            {
                logger.Trace?.Log( lockEvent.ToString() );
            }
        };

        return service;
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
