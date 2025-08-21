// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
        => _serviceProvider ?? throw new InvalidOperationException( "BackstageServiceFactory.Initialize method has not been called." );

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

    public static IServiceProvider InitializeBackstageServices( this IServiceProvider serviceProvider )
    {
        serviceProvider.GetRequiredBackstageService<BackstageServicesInitializer>().Initialize();
        serviceProvider.GetRequiredBackstageService<ShutdownService>().Initialize();

        return serviceProvider;
    }

    public static IServiceProvider CreateServiceProvider( BackstageInitializationOptions options )
    {
        var serviceProviderBuilder = new SimpleServiceProviderBuilder();
        serviceProviderBuilder.AddBackstageServices( options );

        var serviceProvider = serviceProviderBuilder.ServiceProvider;
        serviceProvider.InitializeBackstageServices();

        return serviceProvider;
    }
    
    public static ILicenseConsumptionService CreateTestLicenseConsumptionService( IServiceProvider serviceProvider, string? licenseKey )
    {
        var sources = licenseKey == null
            ? Array.Empty<ExplicitLicenseSource>()
            : new[] { new ExplicitLicenseSource( licenseKey, LicenseSourceKind.UserProfile, serviceProvider ) };

        var service = new LicenseConsumptionService( serviceProvider, sources );

        return service;
    }
}