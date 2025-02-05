// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Newtonsoft.Json;
using System;
using System.Collections.Immutable;

namespace Metalama.Backstage.Diagnostics;

public record LoggingConfiguration
{
    /// <summary>
    /// Gets a value indicating whether logging is enabled at all.
    /// </summary>
    [JsonProperty( "processes" )]
    public ImmutableDictionary<string, bool> Processes { get; init; } =
        ImmutableDictionary<string, bool>.Empty.WithComparers( StringComparer.OrdinalIgnoreCase );

    /// <summary>
    /// Gets the list of categories that are enabled for trace-level logging.
    /// </summary>
    [JsonProperty( "trace" )]
    public ImmutableDictionary<string, bool> TraceCategories { get; init; } =
        ImmutableDictionary<string, bool>.Empty.WithComparers( StringComparer.OrdinalIgnoreCase );

    /// <summary>
    /// Gets the logging duration in hours before it is automatically disabled.
    /// </summary>
    [JsonProperty( "stopLoggingAfterHours" )]
    public double StopLoggingAfterHours { get; init; } = 2;

    public bool IsTraceCategoryEnabled( string category )
        => (this.TraceCategories.TryGetValue( "*", out var allEnabled ) && allEnabled) ||
           (this.TraceCategories.TryGetValue( category, out var enabled ) && enabled);
}