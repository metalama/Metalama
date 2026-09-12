// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.Configuration;

/// <summary>
/// Extension methods that register the configuration services in a <see cref="ServiceProviderBuilder"/>.
/// </summary>
public static class RegisterConfigurationServices
{
    /// <summary>
    /// Registers the file-based <see cref="IConfigurationManager"/>. It requires the core services.
    /// </summary>
    public static ServiceProviderBuilder AddConfigurationServices( this ServiceProviderBuilder serviceProviderBuilder )
        => serviceProviderBuilder.AddSingleton<IConfigurationManager>( serviceProvider => new ConfigurationManager( serviceProvider ) );
}
