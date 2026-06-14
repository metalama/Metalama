// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Desktop.Windows.Commands;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.UserInterface.Toasts;
using Metalama.Backstage.Utilities;
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
        else if ( settings.Kind == ToastNotificationKinds.TelemetryNotice.Name )
        {
            viewModel = new NotificationViewModel(
                settings.Kind,
                "Welcome to Metalama",
                """
                To improve the product, Metalama collects anonymous usage data. Click to learn more or opt out.
                """,
                new CommandActionViewModel( "Privacy options", activationArguments.OpenPrivacyOptions ),
                new UriActionViewModel( "Learn more", webLinks.DisableTelemetryInstructions ) ) { CanMute = false, CanSnooze = false };

            return true;
        }
        else if ( settings.Kind == ToastNotificationKinds.News.Name )
        {
            // Defense in depth: ensure the URI uses a safe (http/https) scheme before it reaches Windows protocol
            // activation. The primary validation is in RssClient, but the URI is server-controlled, so we re-check here.
            // See issue #1647.
            if ( !UrlHelper.IsSafe( settings.Uri, out var newsUri ) )
            {
                viewModel = null;

                return false;
            }

            viewModel = new NotificationViewModel(
                settings.Kind,
                "Metalama Blog Update",
                settings.Title,
                new UriActionViewModel( "Read", newsUri ),
                new CommandActionViewModel( "Options", activationArguments.OpenRssOptions ) ) { CanMute = false, CanSnooze = false };

            return true;
        }
        else
        {
            viewModel = null;

            return false;
        }
    }
}