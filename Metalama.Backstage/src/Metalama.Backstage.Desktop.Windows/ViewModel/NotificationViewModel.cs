// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Desktop.Windows.ViewModel;

// ReSharper disable once NotAccessedPositionalProperty.Global
internal record NotificationViewModel( string Kind, string Title, string Body, NotificationActionViewModel Action, bool CanSnooze = true, bool CanMute = true );