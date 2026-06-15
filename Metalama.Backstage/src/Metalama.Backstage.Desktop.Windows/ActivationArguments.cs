// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Desktop.Windows.Commands;

namespace Metalama.Backstage.Desktop.Windows;

internal sealed class ActivationArguments
{
    private readonly string _options;
    private readonly string _kind;
    private readonly string? _uri;

    public ActivationArguments( NotifyCommandSettings settings )
    {
        this._options = settings.IsDevelopmentEnvironment ? "--dev" : "";
        this._kind = settings.Kind;
        this._uri = settings.Uri;
    }

    public string Mute => $"{MuteNotificationCommand.Name} {this._kind} {this._options}";

    public string Snooze => $"{SnoozeNotificationCommand.Name} {this._kind} {this._options}";

    public string Setup => $"{SetupWizardCommand.Name} {this._options}";

    public string OpenRssOptions => $"{OpenWorkerRssOptionsCommand.Name} {this._options}";

    // The toast Uri carries the worker review-page path (token-safe, no spaces). The activation argument is later split
    // on spaces, so the page path stays a single argument. See #1674.
    public string OpenExceptionReport => $"{OpenWorkerExceptionReportCommand.Name} {this._uri} {this._options}";
}