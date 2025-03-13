// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Immutable;
using System.ComponentModel;

namespace Metalama.Backstage.UserInterface;

[ConfigurationFile( "toastNotifications.json" )]
[Description( "Toast notifications." )]
public sealed record ToastNotificationsConfiguration : ConfigurationFile
{
    [JsonProperty( "pauses" )]
    public ImmutableDictionary<string, DateTime> Pauses { get; init; } = ImmutableDictionary<string, DateTime>.Empty;

    [JsonProperty( "notifications" )]
    public ImmutableDictionary<string, ToastNotificationConfiguration> Notifications { get; init; } =
        ImmutableDictionary<string, ToastNotificationConfiguration>.Empty.WithComparers( StringComparer.OrdinalIgnoreCase );
}