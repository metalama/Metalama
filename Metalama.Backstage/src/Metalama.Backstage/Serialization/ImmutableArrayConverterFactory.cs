// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metalama.Backstage.Serialization;

internal sealed class ImmutableArrayConverterFactory : JsonConverterFactory
{
    public override bool CanConvert( Type typeToConvert )
    {
        if ( !typeToConvert.IsGenericType )
        {
            return false;
        }

        return typeToConvert.GetGenericTypeDefinition() == typeof(ImmutableArray<>);
    }

    public override JsonConverter CreateConverter( Type typeToConvert, JsonSerializerOptions options )
    {
        var elementType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(ImmutableArrayConverter<>).MakeGenericType( elementType );

        return (JsonConverter) Activator.CreateInstance( converterType )!;
    }
}

internal sealed class ImmutableArrayConverter<T> : JsonConverter<ImmutableArray<T>>
{
    public override ImmutableArray<T> Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
    {
        if ( reader.TokenType == JsonTokenType.Null )
        {
            return ImmutableArray<T>.Empty;
        }

        var array = JsonSerializer.Deserialize<T[]>( ref reader, options );

        if ( array == null )
        {
            return ImmutableArray<T>.Empty;
        }

        return ImmutableArray.Create( array );
    }

    public override void Write( Utf8JsonWriter writer, ImmutableArray<T> value, JsonSerializerOptions options )
    {
        if ( value.IsDefault )
        {
            writer.WriteNullValue();

            return;
        }

        writer.WriteStartArray();

        foreach ( var item in value )
        {
            JsonSerializer.Serialize( writer, item, options );
        }

        writer.WriteEndArray();
    }
}
