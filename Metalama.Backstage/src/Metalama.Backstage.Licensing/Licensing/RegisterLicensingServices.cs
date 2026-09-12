// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Audit;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Licensing.Registration;

namespace Metalama.Backstage.Licensing;

/// <summary>
/// Extension methods that register the licensing services in a <see cref="ServiceProviderBuilder"/>.
/// </summary>
public static class RegisterLicensingServices
{
    /// <summary>
    /// Registers the licensing services: the product catalog, the licensing authorities, license consumption and
    /// registration, and the audit when the application enables it. It requires the core, configuration and
    /// telemetry services.
    /// </summary>
    public static ServiceProviderBuilder AddLicensingServices(
        this ServiceProviderBuilder serviceProviderBuilder,
        LicensingInitializationOptions options,
        IApplicationInfo applicationInfo )
    {
        serviceProviderBuilder.AddSingleton( options.ProductCatalog );

        serviceProviderBuilder.AddSingleton<ILicensingAuthorityProvider>(
            serviceProvider => options.UseTestAuthority
                ? new TestLicensingAuthorityProvider( serviceProvider )
                : options.AuthorityProviderFactory( serviceProvider ) );

        if ( applicationInfo.IsLicenseAuditEnabled )
        {
            serviceProviderBuilder.AddSingleton<ILicenseAuditManager>( serviceProvider => new LicenseAuditManager( serviceProvider ) );
        }

        serviceProviderBuilder.AddSingleton( serviceProvider => LicenseConsumptionServiceFactory.Create( serviceProvider, options ) );
        serviceProviderBuilder.AddSingleton<ILicenseRegistrationService>( serviceProvider => new LicenseRegistrationService( serviceProvider ) );

        return serviceProviderBuilder;
    }
}
