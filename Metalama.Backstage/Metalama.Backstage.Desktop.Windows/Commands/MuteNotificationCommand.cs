// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.UserInterface;

namespace Metalama.Backstage.Desktop.Windows.Commands;

[UsedImplicitly( ImplicitUseTargetFlags.WithMembers )]
internal class MuteNotificationCommand : BaseCommand<MuteNotificationCommandSettings>
{
    public const string Name = "mute";

    protected override int Execute( ExtendedCommandContext context, MuteNotificationCommandSettings settings )
    {
        if ( !ToastNotificationKinds.All.TryGetValue( settings.Kind, out var kind ) )
        {
            context.Logger.Error?.Log( $"Invalid notification kind: {settings.Kind}." );

            return 1;
        }

        context.ServiceProvider.GetRequiredBackstageService<IToastNotificationStatusService>().Mute( kind );

        return 0;
    }
}