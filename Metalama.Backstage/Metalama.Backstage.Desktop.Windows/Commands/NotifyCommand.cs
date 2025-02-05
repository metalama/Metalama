// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Desktop.Windows.ViewModel;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.IO;

namespace Metalama.Backstage.Desktop.Windows.Commands;

[UsedImplicitly( ImplicitUseTargetFlags.WithMembers )]
public sealed class NotifyCommand : BaseCommand<NotifyCommandSettings>
{
    public const string Name = "notify";

    protected override int Execute( ExtendedCommandContext context, NotifyCommandSettings settings )
    {
        var logger = context.Logger;

        if ( !ViewModelBuilder.TryGetNotificationViewModel( context.ServiceProvider, settings, out var notificationViewModel ) )
        {
            return -1;
        }

        var activationArguments = new ActivationArguments( settings );
        var builder = new ToastContentBuilder();

        // TODO: serve a different icon in light theme (this one is for the dark theme). The different is only slight.
        var logoPath = Path.Combine( Path.GetDirectoryName( Environment.GetCommandLineArgs()[0] )!, "Resources", "logo.png" );
        builder.AddAppLogoOverride( new Uri( "file:///" + logoPath ) );
        builder.SetToastDuration( ToastDuration.Long );

        builder.AddVisualChild( new AdaptiveText() { Text = notificationViewModel.Title, HintStyle = AdaptiveTextStyle.Title } );

        builder.AddVisualChild( new AdaptiveText() { Text = notificationViewModel.Body, HintMinLines = 4, HintStyle = AdaptiveTextStyle.Body } );

        switch ( notificationViewModel.Action )
        {
            case CommandActionViewModel commandAction:
                builder.AddArgument( commandAction.Command );
                builder.AddButton( commandAction.Text, ToastActivationType.Foreground, commandAction.Command );

                break;

            case UriActionViewModel uriActionViewModel:
                builder.SetProtocolActivation( uriActionViewModel.Uri );
                builder.AddButton( uriActionViewModel.Text, ToastActivationType.Protocol, uriActionViewModel.Uri.ToString() );

                break;
        }

        builder.AddButton( "Snooze", ToastActivationType.Foreground, activationArguments.Snooze );
        builder.AddButton( "Mute", ToastActivationType.Foreground, activationArguments.Mute );

        logger.Trace?.Log( builder.Content.GetXml().GetXml() );

        builder.Show();

        return 0;
    }
}