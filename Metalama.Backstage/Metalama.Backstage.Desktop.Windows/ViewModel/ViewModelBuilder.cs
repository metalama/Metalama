// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Desktop.Windows.Commands;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.UserInterface;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Desktop.Windows.ViewModel;

internal static class ViewModelBuilder
{
    public static bool TryGetNotificationViewModel(
        IServiceProvider serviceProvider,
        NotifyCommandSettings settings,
        [NotNullWhen( true )] out NotificationViewModel? viewModel )
    {
        var activationArguments = new ActivationArguments( settings );
        var webLinks = serviceProvider.GetRequiredBackstageService<WebLinks>();

        if ( settings.Kind == ToastNotificationKinds.RequiresLicense.Name )
        {
            viewModel = new NotificationViewModel(
                settings.Kind,
                "Metalama Professional",
                """
                This project uses a premium Metalama feature. Try Metalama Professional for 45 days or register a license key.
                """,
                new CommandActionViewModel( "Options", activationArguments.Setup ) );

            return true;
        }
        else if ( settings.Kind == ToastNotificationKinds.VsxNotInstalled.Name )
        {
            viewModel = new NotificationViewModel(
                settings.Kind,
                "Install Metalama Tools for Visual Studio",
                """
                to enhance your Metalama coding experience: syntax highlighting, CodeLens, and diff preview.
                """,
                new UriActionViewModel( "Install", webLinks.InstallVsx ) );

            return true;
        }
        else if ( settings.Kind == ToastNotificationKinds.LicenseExpiring.Name )
        {
            viewModel = new NotificationViewModel(
                settings.Kind,
                settings.Title ?? "Your Metalama license is expiring",
                settings.Text ?? "Renew your Metalama subscription",
                new UriActionViewModel( "Renew", webLinks.RenewSubscription ) );

            return true;
        }
        else if ( settings.Kind == ToastNotificationKinds.TrialExpiring.Name )
        {
            viewModel = new NotificationViewModel(
                settings.Kind,
                settings.Title ?? "Your Metalama trial is expiring",
                settings.Text ?? "Register a license key or activate Metalama Free.",
                new CommandActionViewModel( "Open", activationArguments.Setup ) );

            return true;
        }
        else if ( settings.Kind == ToastNotificationKinds.SubscriptionExpiring.Name )
        {
            viewModel = new NotificationViewModel(
                settings.Kind,
                settings.Title ?? "Your Metalama subscription is expiring",
                settings.Text ?? "Renew your subscription to benefit from continued updates and support.",
                new CommandActionViewModel( "Open", activationArguments.Setup ) );

            return true;
        }
        else if ( settings.Kind == ToastNotificationKinds.Exception.Name )
        {
            viewModel = new NotificationViewModel(
                settings.Kind,
                settings.Title ?? "Metalama failed",
                settings.Text ?? "Metalama encountered an unhandled exception.",
                new UriActionViewModel( "View", settings.Uri! ) );

            return true;
        }
        else if ( settings.Kind == ToastNotificationKinds.Welcome.Name )
        {
            viewModel = new NotificationViewModel(
                settings.Kind,
                settings.Title ?? "Welcome to Metalama",
                settings.Text
                ?? "Thank you for using Metalama.\nNote that telemetry is enabled by default. Open this notification to learn how to disable telemetry.",
                new UriActionViewModel( "View", webLinks.DisableTelemetryInstructions ),
                false,
                false );

            return true;
        }
        else
        {
            viewModel = null;

            return false;
        }
    }
}