// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Newtonsoft.Json;
using System;
using System.Collections.Immutable;

namespace Metalama.Backstage.Diagnostics;

public sealed record ProfilingConfiguration
{
    /// <summary>
    /// Gets or sets the kind of profiling. Possible values
    /// are: <c>performance</c>, <c>memory</c> and <c>memory-allocation</c>.
    /// </summary>
    [JsonProperty( "kind" )]
    public string? Kind { get; set; }

    /// <summary>
    /// Gets a value indicating whether profiling is enabled.
    /// </summary>
    [JsonProperty( "processes" )]
    public ImmutableDictionary<string, bool> Processes { get; init; } =
        ImmutableDictionary<string, bool>.Empty.WithComparers( StringComparer.OrdinalIgnoreCase );
}