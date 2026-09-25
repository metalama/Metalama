// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Tools;
using Metalama.Framework.ConfigurationFiles;
using SharpCrafters.Backstage.Extensibility;
using System;

namespace Metalama.Framework.Engine.Utilities.Diagnostics;

/// <summary>
/// Creates and holds the provider of the Backstage services of the current process.
/// </summary>
/// <remarks>
/// <para>
/// Metalama runs in hosts that do not give it a container of its own, such as the compiler, the Roslyn analysis
/// process and the LinqPad driver. The entry point of each host calls <see cref="Initialize"/> once, and the code
/// that runs before a Metalama service provider exists reads <see cref="ServiceProvider"/>.
/// </para>
/// <para>
/// The provider is created with <see cref="BackstageServiceFactory.CreateServiceProvider"/>. The process-wide provider
/// of <see cref="BackstageServiceFactory"/> is obsolete and is not used.
/// </para>
/// </remarks>
public static class BackstageServiceFactoryInitializer
{
    private static readonly object _initializeSync = new();
    private static volatile IServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets a value indicating whether <see cref="Initialize"/> has been called.
    /// </summary>
    [PublicAPI]
    public static bool IsInitialized => _serviceProvider != null;

    /// <summary>
    /// Gets the provider of the Backstage services of the current process.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has not been called.</exception>
    public static IServiceProvider ServiceProvider
        => _serviceProvider ?? throw new InvalidOperationException( $"{nameof(BackstageServiceFactoryInitializer)}.{nameof(Initialize)} has not been called." );

    private static BackstageInitializationOptions WithToolsAndFrameworkJsonContext( BackstageInitializationOptions options )
        => options with
        {
            AddToolsExtractor = builder => builder.AddTools(),
            AdditionalJsonTypeInfoResolvers = [FrameworkConfigurationJsonContext.Default]
        };

    private static void InitializeMetalamaServices() => Logger.Initialize();

    /// <summary>
    /// Creates the provider of the Backstage services of the current process, unless it has already been created.
    /// </summary>
    /// <param name="options">The options of the provider.</param>
    /// <returns><see langword="true"/> if this call created the provider, or <see langword="false"/> if it already existed.</returns>
    public static bool Initialize( BackstageInitializationOptions options )
    {
        lock ( _initializeSync )
        {
            if ( _serviceProvider != null )
            {
                return false;
            }

            _serviceProvider = BackstageServiceFactory.CreateServiceProvider( WithToolsAndFrameworkJsonContext( options ) );
        }

        InitializeMetalamaServices();

        return true;
    }
}
