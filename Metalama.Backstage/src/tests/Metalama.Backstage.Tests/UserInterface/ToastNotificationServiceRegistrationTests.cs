// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Tests.Extensibility;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Tools;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.UserInterface.Toasts;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace Metalama.Backstage.Tests.UserInterface;

/// <summary>
/// Regression tests for issue #2047. The Windows notification platform can refuse to serve the process, which is an
/// ordinary state of a machine and not an exceptional one. The toast notification service must therefore be registered
/// only when toast notifications are supported, so that consumers, which already resolve it as an optional service,
/// simply display nothing instead of launching a desktop tool that crashes with a <c>COMException</c>.
/// </summary>
public sealed class ToastNotificationServiceRegistrationTests
{
    private static IServiceProvider BuildServiceProvider( bool areToastNotificationsSupported )
    {
        var options =
            new BackstageInitializationOptions( new TestApplicationInfo( "Test", true, "1.0", DateTime.Today ) )
            {
                AddUserInterface = true,
                DetectToastNotifications = true,
                AddToolsExtractor = b => b.AddService( typeof(IBackstageToolsExtractor), p => new BackstageToolsExtractor( p ) )
            };

        var serviceCollectionBuilder = new ServiceCollectionBuilder();

        serviceCollectionBuilder.ServiceCollection.AddSingleton<IUserDeviceDetectionService>(
            new TestUserDeviceDetectionService { IsInteractiveDevice = areToastNotificationsSupported } );

        serviceCollectionBuilder.AddBackstageServices( options );

        return serviceCollectionBuilder.ServiceCollection.BuildServiceProvider();
    }

    [Fact]
    public void ToastNotificationServiceIsRegisteredWhenSupported()
    {
        var serviceProvider = BuildServiceProvider( true );

        Assert.NotNull( serviceProvider.GetBackstageService<IToastNotificationService>() );
    }

    /// <summary>
    /// Asserts that the toast notification service is absent when the device cannot display a toast notification.
    /// Without this check the service is registered unconditionally, the desktop notification tool is launched, and
    /// the tool crashes inside the Windows Runtime interop.
    /// </summary>
    [Fact]
    public void ToastNotificationServiceIsNotRegisteredWhenNotSupported()
    {
        var serviceProvider = BuildServiceProvider( false );

        Assert.Null( serviceProvider.GetBackstageService<IToastNotificationService>() );
    }
}
