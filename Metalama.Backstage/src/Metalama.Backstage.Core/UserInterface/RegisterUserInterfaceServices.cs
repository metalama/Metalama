// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.UserInterface.Rss;
using Metalama.Backstage.UserInterface.Toasts;
using Metalama.Backstage.Welcome;
using System.Runtime.InteropServices;

namespace Metalama.Backstage.UserInterface;

/// <summary>
/// Extension methods that register the user interface services in a <see cref="ServiceProviderBuilder"/>.
/// </summary>
public static class RegisterUserInterfaceServices
{
    /// <summary>
    /// Registers the user interface services: the notifications, the welcome page, the news client when requested,
    /// and the shell of the current operating system. It requires the core, configuration and telemetry services,
    /// and the licensing services when the notifications about licenses are wanted.
    /// </summary>
    public static ServiceProviderBuilder AddUserInterfaceServices(
        this ServiceProviderBuilder serviceProviderBuilder,
        UserInterfaceInitializationOptions options,
        IWebLinks webLinks )
    {
        serviceProviderBuilder
            .AddSingleton( options )
            .AddSingleton<IWebLinks>( webLinks );

        if ( options.OpenWelcomePage )
        {
            serviceProviderBuilder.AddSingleton( serviceProvider => new WelcomePageService( serviceProvider ) );
        }

        serviceProviderBuilder
            .AddSingleton<IToastNotificationStatusService>( serviceProvider => new ToastNotificationStatusService( serviceProvider ) )
            .AddSingleton<IToastNotificationService>( serviceProvider => new ToastNotificationService( serviceProvider ) )
            .AddSingleton( serviceProvider => new UserInterfaceEventSubscriber( serviceProvider ) );

        if ( options.DetectToastNotifications )
        {
            serviceProviderBuilder.AddSingleton<IToastNotificationDetectionService>( serviceProvider => new ToastNotificationDetectionService( serviceProvider ) );
        }

        if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            serviceProviderBuilder
                .AddSingleton<IIdeExtensionStatusService>( serviceProvider => new IdeExtensionStatusService( serviceProvider ) )
                .AddSingleton<IUserInterfaceService>( serviceProvider => new WindowsUserInterfaceService( serviceProvider ) );
        }
        else if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
        {
            serviceProviderBuilder.AddSingleton<IUserInterfaceService>( serviceProvider => new LinuxUserInterfaceService( serviceProvider ) );
        }
        else
        {
            serviceProviderBuilder.AddSingleton<IUserInterfaceService>( serviceProvider => new BrowserBasedUserInterfaceService( serviceProvider ) );
        }

        if ( options.AddRssClient )
        {
            serviceProviderBuilder.AddSingleton<IRssClient>( serviceProvider => new RssClient( serviceProvider ) );
        }

        return serviceProviderBuilder;
    }

    /// <summary>
    /// Registers the news client alone, for a host that does not register the other user interface services. It
    /// requires the core, configuration and telemetry services.
    /// </summary>
    public static ServiceProviderBuilder AddRssClientServices(
        this ServiceProviderBuilder serviceProviderBuilder,
        UserInterfaceInitializationOptions options,
        IWebLinks webLinks )
        => serviceProviderBuilder
            .AddSingleton( options )
            .AddSingleton<IWebLinks>( webLinks )
            .AddSingleton<IRssClient>( serviceProvider => new RssClient( serviceProvider ) );
}
