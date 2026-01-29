// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metalama.Backstage.Serialization;

internal sealed class ImmutableDictionaryConverterFactory : JsonConverterFactory
{
    public override bool CanConvert( Type typeToConvert )
    {
        if ( !typeToConvert.IsGenericType )
        {
            return false;
        }

        return typeToConvert.GetGenericTypeDefinition() == typeof(ImmutableDictionary<,>);
    }

    public override JsonConverter CreateConverter( Type typeToConvert, JsonSerializerOptions options )
    {
        var keyType = typeToConvert.GetGenericArguments()[0];
        var valueType = typeToConvert.GetGenericArguments()[1];

        var converterType = typeof(ImmutableDictionaryConverter<,>).MakeGenericType( keyType, valueType );

        return (JsonConverter) Activator.CreateInstance( converterType )!;
    }
}

internal sealed class ImmutableDictionaryConverter<TKey, TValue> : JsonConverter<ImmutableDictionary<TKey, TValue>>
    where TKey : notnull
{
    public override ImmutableDictionary<TKey, TValue>? Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
    {
        if ( reader.TokenType == JsonTokenType.Null )
        {
            return null;
        }

        if ( reader.TokenType != JsonTokenType.StartObject )
        {
            throw new JsonException( $"Expected start of object, but got {reader.TokenType}" );
        }

        var builder = ImmutableDictionary.CreateBuilder<TKey, TValue>();

        // For string keys with case-insensitive comparison
        if ( typeof(TKey) == typeof(string) )
        {
            builder.KeyComparer = (IEqualityComparer<TKey>) (object) StringComparer.OrdinalIgnoreCase;
        }

        var valueConverter = (JsonConverter<TValue>) options.GetConverter( typeof(TValue) );

        while ( reader.Read() )
        {
            if ( reader.TokenType == JsonTokenType.EndObject )
            {
                return builder.ToImmutable();
            }

            if ( reader.TokenType != JsonTokenType.PropertyName )
            {
                throw new JsonException( $"Expected property name, but got {reader.TokenType}" );
            }

            var keyString = reader.GetString();

            if ( keyString == null )
            {
                throw new JsonException( "Dictionary key cannot be null" );
            }

            // Read the value
            reader.Read();

            TValue value;

            if ( valueConverter != null )
            {
                value = valueConverter.Read( ref reader, typeof(TValue), options )!;
            }
            else
            {
                value = JsonSerializer.Deserialize<TValue>( ref reader, options )!;
            }

            // Convert string key to TKey
            TKey key;

            if ( typeof(TKey) == typeof(string) )
            {
                key = (TKey) (object) keyString;
            }
            else if ( typeof(TKey) == typeof(long) )
            {
                key = (TKey) (object) long.Parse( keyString );
            }
            else if ( typeof(TKey) == typeof(int) )
            {
                key = (TKey) (object) int.Parse( keyString );
            }
            else
            {
                throw new JsonException( $"ImmutableDictionary with key type {typeof(TKey)} is not supported." );
            }

            builder.Add( key, value );
        }

        throw new JsonException( "Unexpected end of JSON" );
    }

    public override void Write( Utf8JsonWriter writer, ImmutableDictionary<TKey, TValue> value, JsonSerializerOptions options )
    {
        writer.WriteStartObject();

        var valueConverter = (JsonConverter<TValue>?) options.GetConverter( typeof(TValue) );

        foreach ( var kvp in value )
        {
            // Convert key to string for JSON property name
            string propertyName;

            if ( typeof(TKey) == typeof(string) )
            {
                propertyName = (string) (object) kvp.Key;

                // Apply naming policy if present (only for string keys)
                if ( options.PropertyNamingPolicy != null )
                {
                    propertyName = options.PropertyNamingPolicy.ConvertName( propertyName );
                }
            }
            else
            {
                // For non-string keys, convert to string using ToString()
                propertyName = kvp.Key.ToString()!;
            }

            writer.WritePropertyName( propertyName );

            if ( kvp.Value == null )
            {
                writer.WriteNullValue();
            }
            else if ( valueConverter != null )
            {
                valueConverter.Write( writer, kvp.Value, options );
            }
            else
            {
                JsonSerializer.Serialize( writer, kvp.Value, options );
            }
        }

        writer.WriteEndObject();
    }
}
