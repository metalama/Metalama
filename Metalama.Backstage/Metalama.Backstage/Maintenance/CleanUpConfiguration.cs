// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Newtonsoft.Json;
using System;

namespace Metalama.Backstage.Maintenance;

[ConfigurationFile( "cleanup.json" )]
public sealed record CleanUpConfiguration : ConfigurationFile
{
#pragma warning disable CA1507 // Use nameof in place of string literal
    [JsonProperty( "LastCleanUpTime" )]
#pragma warning restore CA1507
    public DateTime? LastCleanUpTime { get; init; }
}