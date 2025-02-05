// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
                "Activate Metalama",
                """
                Choose between Metalama Free,
                a 45-day trial of Metalama Ultimate, or register a license key.
                """,
                new CommandActionViewModel( "Activate", activationArguments.Setup ) );

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
        else
        {
            viewModel = null;

            return false;
        }
    }
}