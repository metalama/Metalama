// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Newtonsoft.Json;
using System;

namespace Metalama.Backstage.UserInterface;

public record ToastNotificationConfiguration
{
    [JsonProperty( "snoozeUntil" )]
    public DateTime? SnoozeUntil { get; init; }

    [JsonProperty( "disabled" )]
    public bool Disabled { get; init; }
}