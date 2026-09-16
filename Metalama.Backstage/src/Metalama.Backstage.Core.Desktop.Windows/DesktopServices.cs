// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Desktop.Windows.Commands;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Desktop.Windows;

/// <summary>
/// Creates the Backstage services of the notifier process in the process-wide <see cref="BackstageServiceFactory"/>
/// from the application info that the product supplies to <see cref="BackstageDesktopProgram.Run"/>.
/// </summary>
internal static class DesktopServices
{
    private static BackstageDesktopApplicationInfo? _applicationInfo;

    /// <summary>
    /// Gets the service provider when it has been created, or <c>null</c> when no command has requested it yet.
    /// </summary>
    public static IServiceProvider? ServiceProvider => BackstageServiceFactory.IsInitialized ? BackstageServiceFactory.ServiceProvider : null;

    /// <summary>
    /// Stores the application info. It is called once by <see cref="BackstageDesktopProgram.Run"/>.
    /// </summary>
    public static void Initialize( BackstageDesktopApplicationInfo applicationInfo )
    {
        if ( _applicationInfo != null )
        {
            throw new InvalidOperationException( "The desktop services have already been initialized." );
        }

        _applicationInfo = applicationInfo;
    }

    /// <summary>
    /// Gets the service provider, creating it on the first call with the settings of the command.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has not been called.</exception>
    public static IServiceProvider Get( BaseSettings settings )
    {
        var applicationInfo = _applicationInfo
                              ?? throw new InvalidOperationException(
                                  $"{nameof(BackstageDesktopProgram)}.{nameof(BackstageDesktopProgram.Run)} has not been called." );

        BackstageServiceFactory.Initialize(
            new BackstageInitializationOptions( applicationInfo, applicationInfo.Product )
            {
                AddLicensing = false,
                IsDevelopmentEnvironment = settings.IsDevelopmentEnvironment,
                AddSupportServices = true,
                AddUserInterface = true,

                // We don't want to open more toast notifications.
                DetectToastNotifications = false
            },
            applicationInfo.Name );

        var serviceProvider = BackstageServiceFactory.ServiceProvider;
        var logger = serviceProvider.GetLoggerFactory().GetLogger( "App" );
        logger.Trace?.Log( $"Executing: {string.Join( ' ', Environment.GetCommandLineArgs() )}" );

        return serviceProvider;
    }
}
