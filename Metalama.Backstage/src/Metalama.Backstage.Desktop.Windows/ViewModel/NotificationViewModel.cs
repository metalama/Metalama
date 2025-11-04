// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Desktop.Windows.ViewModel;

// ReSharper disable once NotAccessedPositionalProperty.Global
internal sealed record NotificationViewModel
{
    public NotificationViewModel( string kind, string title, string? body, params NotificationActionViewModel[] actions )
    {
        this.Kind = kind;
        this.Title = title;
        this.Body = body;
        this.Actions = actions;
    }

    public string Kind { get; }

    public string Title { get; }

    public string? Body { get; }

    public NotificationActionViewModel[] Actions { get; }

    public bool CanSnooze { get; init; } = true;

    public bool CanMute { get; init; } = true;
}