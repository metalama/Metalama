// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Newtonsoft.Json;
using System;
using System.Text.Json.Serialization;

namespace Metalama.Backstage.UserInterface.Toasts;

public sealed record ToastNotificationConfiguration
{
    [JsonProperty( "snoozeUntil" )]
    [JsonPropertyName( "snoozeUntil" )]
    public DateTime? SnoozeUntil { get; init; }

    [JsonProperty( "disabled" )]
    [JsonPropertyName( "disabled" )]
    public bool Disabled { get; init; }
}