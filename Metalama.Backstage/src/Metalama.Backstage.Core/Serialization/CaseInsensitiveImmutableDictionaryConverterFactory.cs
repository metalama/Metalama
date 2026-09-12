// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metalama.Backstage.Serialization;

/// <summary>
/// A generic JSON converter factory for <see cref="ImmutableDictionary{TKey, TValue}"/> with string keys
/// that uses <see cref="StringComparer.OrdinalIgnoreCase"/>.
/// Apply this converter via <c>[JsonConverter(typeof(CaseInsensitiveImmutableDictionaryConverterFactory&lt;TValue&gt;))]</c>
/// on properties that need case-insensitive key comparison.
/// </summary>
/// <typeparam name="TValue">The dictionary value type.</typeparam>
public sealed class CaseInsensitiveImmutableDictionaryConverterFactory<TValue> : JsonConverterFactory
{
    public override bool CanConvert( Type typeToConvert ) => typeToConvert == typeof(ImmutableDictionary<string, TValue>);

    public override JsonConverter CreateConverter( Type typeToConvert, JsonSerializerOptions options )
        => new CaseInsensitiveImmutableDictionaryConverter<TValue>();
}