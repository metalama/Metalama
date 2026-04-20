// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.Text;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metalama.Framework.Engine.Serialization;

/// <summary>
/// System.Text.Json converter for <see cref="TextSpan"/>.
/// </summary>
internal sealed class TextSpanJsonConverter : JsonConverter<TextSpan>
{
    public override TextSpan Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
    {
        if ( reader.TokenType != JsonTokenType.StartObject )
        {
            throw new JsonException( "Expected StartObject token." );
        }

        var start = 0;
        var length = 0;

        while ( reader.Read() )
        {
            if ( reader.TokenType == JsonTokenType.EndObject )
            {
                break;
            }

            if ( reader.TokenType != JsonTokenType.PropertyName )
            {
                throw new JsonException( "Expected PropertyName token." );
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch ( propertyName )
            {
                case "start":
                case "Start":
                    start = reader.GetInt32();

                    break;

                case "length":
                case "Length":
                    length = reader.GetInt32();

                    break;
            }
        }

        return new TextSpan( start, length );
    }

    public override void Write( Utf8JsonWriter writer, TextSpan value, JsonSerializerOptions options )
    {
        writer.WriteStartObject();
        writer.WriteNumber( "start", value.Start );
        writer.WriteNumber( "length", value.Length );
        writer.WriteEndObject();
    }
}