// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Sources;
using System;

namespace Metalama.Backstage.Extensibility;

[PublicAPI]
public static class BackstageServiceFactory
{
    private static readonly object _initializeSync = new();

    private static IServiceProvider? _serviceProvider;

    public static IServiceProvider ServiceProvider
        => _serviceProvider ?? throw new InvalidOperationException( "BackstageServiceFactory has not been initialized." );

    public static bool IsInitialized => _serviceProvider != null;

    public static bool Initialize( BackstageInitializationOptions options, string caller )
    {
        lock ( _initializeSync )
        {
            if ( _serviceProvider != null )
            {
                _serviceProvider.GetLoggerFactory()
                    .GetLogger( "BackstageServiceFactory" )
                    .Trace?.Log( $"Support services initialization requested from {caller}. The services are already initialized." );

                return false;
            }

            _serviceProvider = CreateServiceProvider( options );

            _serviceProvider.GetLoggerFactory()
                .GetLogger( "BackstageServiceFactory" )
                .Trace?.Log( $"Support services initialized upon a request from {caller}." );

            return true;
        }
    }
    
    public static IServiceProvider CreateServiceProvider( BackstageInitializationOptions options )
    {
        var serviceProviderBuilder = new SimpleServiceProviderBuilder();
        serviceProviderBuilder.AddBackstageServices( options );

        _serviceProvider = serviceProviderBuilder.ServiceProvider;
        _serviceProvider.GetRequiredBackstageService<ShutdownService>().Initialize();

        if ( options.Initialize )
        {
            _serviceProvider.GetRequiredBackstageService<BackstageServicesInitializer>().Initialize();
        }

        return _serviceProvider;
    }

    public static ILicenseConsumptionService CreateTestLicenseConsumptionService( IServiceProvider serviceProvider, string? licenseKey )
    {
        var sources = licenseKey == null ? Array.Empty<ExplicitLicenseSource>() : new[] { new ExplicitLicenseSource( licenseKey, serviceProvider ) };

        var service = new LicenseConsumptionService( serviceProvider, sources );

        return service;
    }
}