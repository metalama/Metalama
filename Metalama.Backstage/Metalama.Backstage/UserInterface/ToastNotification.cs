// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.UserInterface;

public record ToastNotification( ToastNotificationKind Kind, string? Title = null, string? Text = null, string? Uri = null );