// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metalama.Backstage.Serialization;

/// <summary>
/// A JSON converter for <see cref="ImmutableDictionary{TKey, TValue}"/> with string keys
/// that uses <see cref="StringComparer.OrdinalIgnoreCase"/>.
/// </summary>
/// <typeparam name="TValue">The dictionary value type.</typeparam>
public sealed class CaseInsensitiveImmutableDictionaryConverter<TValue> : JsonConverter<ImmutableDictionary<string, TValue>>
{
    public override ImmutableDictionary<string, TValue>? Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
    {
        if ( reader.TokenType == JsonTokenType.Null )
        {
            return null;
        }

        // Use the built-in deserializer to get a Dictionary
        var dictionary = JsonSerializer.Deserialize<Dictionary<string, TValue>>( ref reader, options );

        if ( dictionary == null )
        {
            return null;
        }

        // Convert to ImmutableDictionary with case-insensitive comparer
        return dictionary.ToImmutableDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value,
            StringComparer.OrdinalIgnoreCase );
    }

    public override void Write( Utf8JsonWriter writer, ImmutableDictionary<string, TValue> value, JsonSerializerOptions options )
    {
        // Write as a JSON object manually to avoid needing IDictionary in the type resolver
        writer.WriteStartObject();

        foreach ( var kvp in value )
        {
            // Write the key as property name
            // Note: Dictionary keys are data, not property names, so we don't apply PropertyNamingPolicy
            // (that policy is for C# property names, not dictionary keys which are user data)
            writer.WritePropertyName( kvp.Key );

            // Write the value using the serializer
            JsonSerializer.Serialize( writer, kvp.Value, options );
        }

        writer.WriteEndObject();
    }
}