// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Desktop.Windows.Commands;

namespace Metalama.Backstage.Desktop.Windows;

internal class ActivationArguments
{
    private readonly string _options;
    private readonly string _kind;

    public ActivationArguments( NotifyCommandSettings settings )
    {
        this._options = settings.IsDevelopmentEnvironment ? "--dev" : "";
        this._kind = settings.Kind;
    }

    public string Mute => $"{MuteNotificationCommand.Name} {this._kind} {this._options}";

    public string Snooze => $"{SnoozeNotificationCommand.Name} {this._kind} {this._options}";

    public string Setup => $"{SetupWizardCommand.Name} {this._options}";
}