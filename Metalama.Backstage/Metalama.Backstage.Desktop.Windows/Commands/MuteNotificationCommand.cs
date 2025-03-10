// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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