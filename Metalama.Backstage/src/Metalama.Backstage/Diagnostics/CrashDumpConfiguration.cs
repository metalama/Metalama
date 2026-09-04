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

public sealed record CrashDumpConfiguration
{
    /// <summary>
    /// Gets a value indicating whether logging is enabled at all.
    /// </summary>
    [JsonPropertyName( "processes" )]
    [JsonConverter( typeof(CaseInsensitiveImmutableDictionaryConverterFactory<bool>) )]
    public ImmutableDictionary<string, bool> Processes { get; init; } =
        ImmutableDictionary<string, bool>.Empty.WithComparers( StringComparer.OrdinalIgnoreCase );

    private readonly ImmutableArray<string> _exceptionTypes = ImmutableArray<string>.Empty;

    /// <summary>
    /// Gets the list of exception types for which a crash dump is collected.
    /// </summary>
    /// <remarks>
    /// The value is normalized to <see cref="ImmutableArray{T}.Empty"/> because a default (uninitialized)
    /// <see cref="ImmutableArray{T}"/> wraps a null array and throws when it is enumerated or serialized. A property
    /// initializer alone is not enough: the System.Text.Json source generator treats every <c>init</c> property as a
    /// constructor parameter and assigns it unconditionally, so JSON without an <c>exceptionTypes</c> entry overwrites
    /// the initializer with the default value.
    /// </remarks>
    [JsonPropertyName( "exceptionTypes" )]
    public ImmutableArray<string> ExceptionTypes
    {
        get => this._exceptionTypes;
        init => this._exceptionTypes = value.IsDefault ? ImmutableArray<string>.Empty : value;
    }

    /// <summary>
    /// Gets or sets the members of the configuration file that this version of Metalama does not declare.
    /// </summary>
    /// <remarks>
    /// See <see cref="Metalama.Backstage.Configuration.ConfigurationFile.UnknownMembers"/> for the reason why this
    /// property exists and why it has a setter rather than an initializer.
    /// </remarks>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? UnknownMembers { get; set; }
}