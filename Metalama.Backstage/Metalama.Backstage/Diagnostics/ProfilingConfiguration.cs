// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Newtonsoft.Json;
using System;
using System.Collections.Immutable;

namespace Metalama.Backstage.Diagnostics;

public record ProfilingConfiguration
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