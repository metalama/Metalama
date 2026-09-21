// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Tests.Extensibility;
using Metalama.Backstage.Tools;
using Metalama.Backstage.UserInterface.Toasts;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace Metalama.Backstage.Tests.UserInterface;

/// <summary>
/// Regression tests for issue #2047, which asserts that the toast notification services are registered only when the
/// machine can display a toast notification.
/// </summary>
/// <remarks>
/// The Windows notification platform declines to serve the process on a Windows installation without the notification
/// platform, in a session with no interactive desktop, and when a policy disables notifications. The desktop
/// notification tool then failed with an unhandled <c>COMException</c>. Consumers resolve the toast notification
/// services as optional services, therefore an absent service means that no notification is displayed.
/// </remarks>
public sealed class ToastNotificationServiceRegistrationTests
{
    private static IServiceProvider BuildServiceProvider( bool areToastNotificationsSupported )
    {
        var options =
            new BackstageInitializationOptions( new TestApplicationInfo( "Test", true, "1.0", DateTime.Today ) )
            {
                AddUserInterface = true,
                DetectToastNotifications = true,
                AreToastNotificationsSupported = areToastNotificationsSupported,
                AddToolsExtractor = b => b.AddService( typeof(IBackstageToolsExtractor), p => new BackstageToolsExtractor( p ) )
            };

        var serviceCollectionBuilder = new ServiceCollectionBuilder();
        serviceCollectionBuilder.AddBackstageServices( options );

        return serviceCollectionBuilder.ServiceCollection.BuildServiceProvider();
    }

    /// <summary>
    /// Asserts that the toast notification services are registered on a machine that can display a toast notification.
    /// </summary>
    [Fact]
    public void ToastNotificationServicesAreRegisteredWhenSupported()
    {
        var serviceProvider = BuildServiceProvider( true );

        Assert.NotNull( serviceProvider.GetBackstageService<IToastNotificationService>() );
        Assert.NotNull( serviceProvider.GetBackstageService<IToastNotificationDetectionService>() );
    }

    /// <summary>
    /// Asserts that the toast notification services are absent on a machine that cannot display a toast notification.
    /// </summary>
    [Fact]
    public void ToastNotificationServicesAreNotRegisteredWhenNotSupported()
    {
        var serviceProvider = BuildServiceProvider( false );

        Assert.Null( serviceProvider.GetBackstageService<IToastNotificationService>() );
        Assert.Null( serviceProvider.GetBackstageService<IToastNotificationDetectionService>() );
    }

    /// <summary>
    /// Asserts that the status service, which the Mute and Snooze commands and the notification settings page require,
    /// is registered even when the machine cannot display a toast notification.
    /// </summary>
    [Fact]
    public void ToastNotificationStatusServiceIsRegisteredWhenNotSupported()
    {
        var serviceProvider = BuildServiceProvider( false );

        Assert.NotNull( serviceProvider.GetBackstageService<IToastNotificationStatusService>() );
    }
}
