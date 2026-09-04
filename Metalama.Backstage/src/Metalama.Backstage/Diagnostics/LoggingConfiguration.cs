// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metalama.Backstage.Diagnostics;

public sealed record LoggingConfiguration
{
    /// <summary>
    /// Gets a value indicating whether logging is enabled at all.
    /// </summary>
    [JsonPropertyName( "processes" )]
    [JsonConverter( typeof(CaseInsensitiveImmutableDictionaryConverterFactory<bool>) )]
    public ImmutableDictionary<string, bool> Processes { get; init; } =
        ImmutableDictionary<string, bool>.Empty.WithComparers( StringComparer.OrdinalIgnoreCase );

    /// <summary>
    /// Gets the list of categories that are enabled for trace-level logging.
    /// </summary>
    [JsonPropertyName( "trace" )]
    [JsonConverter( typeof(CaseInsensitiveImmutableDictionaryConverterFactory<bool>) )]
    public ImmutableDictionary<string, bool> TraceCategories { get; init; } =
        ImmutableDictionary<string, bool>.Empty.WithComparers( StringComparer.OrdinalIgnoreCase );

    /// <summary>
    /// Gets the logging duration in hours before it is automatically disabled.
    /// </summary>
    [JsonPropertyName( "stopLoggingAfterHours" )]
    public double StopLoggingAfterHours { get; init; } = 2;

    /// <summary>
    /// Gets or sets the members of the configuration file that this version of Metalama does not declare.
    /// </summary>
    /// <remarks>
    /// See <see cref="Metalama.Backstage.Configuration.ConfigurationFile.UnknownMembers"/> for the reason why this
    /// property exists and why it has a setter rather than an initializer.
    /// </remarks>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? UnknownMembers { get; set; }

    public bool IsTraceCategoryEnabled( string category )
        => (this.TraceCategories.TryGetValue( "*", out var allEnabled ) && allEnabled) ||
           (this.TraceCategories.TryGetValue( category, out var enabled ) && enabled);
}