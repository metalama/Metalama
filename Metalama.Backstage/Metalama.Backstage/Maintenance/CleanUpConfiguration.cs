// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Newtonsoft.Json;
using System;

namespace Metalama.Backstage.Maintenance;

[ConfigurationFile( "cleanup.json" )]
public record CleanUpConfiguration : ConfigurationFile
{
#pragma warning disable CA1507 // Use nameof in place of string literal
    [JsonProperty( "LastCleanUpTime" )]
#pragma warning restore CA1507
    public DateTime? LastCleanUpTime { get; init; }
}