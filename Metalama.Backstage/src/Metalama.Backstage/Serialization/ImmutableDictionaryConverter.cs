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
/// A JSON converter for <see cref="ImmutableDictionary{TKey, TValue}"/> that wraps the built-in converter
/// and uses <see cref="StringComparer.OrdinalIgnoreCase"/> for string keys.
/// </summary>
internal sealed class ImmutableDictionaryConverter<TKey, TValue> : JsonConverter<ImmutableDictionary<TKey, TValue>>
    where TKey : notnull
{
    public override ImmutableDictionary<TKey, TValue>? Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
    {
        if ( reader.TokenType == JsonTokenType.Null )
        {
            return null;
        }

        // Use the built-in deserializer to get a Dictionary
        var dictionary = JsonSerializer.Deserialize<Dictionary<TKey, TValue>>( ref reader, options );

        if ( dictionary == null )
        {
            return null;
        }

        // Convert to ImmutableDictionary with appropriate comparer
        if ( typeof(TKey) == typeof(string) )
        {
            // Use case-insensitive comparer for string keys
            return dictionary.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value,
                (IEqualityComparer<TKey>) (object) StringComparer.OrdinalIgnoreCase );
        }

        return dictionary.ToImmutableDictionary();
    }

    public override void Write( Utf8JsonWriter writer, ImmutableDictionary<TKey, TValue> value, JsonSerializerOptions options )
    {
        // Write as a JSON object manually to avoid needing IDictionary in the type resolver
        writer.WriteStartObject();

        foreach ( var kvp in value )
        {
            // Write the key as property name
            var keyString = kvp.Key?.ToString() ?? throw new JsonException( "Dictionary key cannot be null" );

            // Note: Dictionary keys are data, not property names, so we don't apply PropertyNamingPolicy
            // (that policy is for C# property names, not dictionary keys which are user data)
            writer.WritePropertyName( keyString );

            // Write the value using the serializer
            JsonSerializer.Serialize( writer, kvp.Value, options );
        }

        writer.WriteEndObject();
    }
}
