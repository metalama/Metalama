// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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