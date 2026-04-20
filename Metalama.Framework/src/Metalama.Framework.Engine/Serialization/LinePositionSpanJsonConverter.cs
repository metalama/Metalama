// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.Text;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metalama.Framework.Engine.Serialization;

/// <summary>
/// System.Text.Json converter for <see cref="LinePositionSpan"/>.
/// </summary>
internal sealed class LinePositionSpanJsonConverter : JsonConverter<LinePositionSpan>
{
    public override LinePositionSpan Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
    {
        if ( reader.TokenType != JsonTokenType.StartObject )
        {
            throw new JsonException( "Expected StartObject token." );
        }

        LinePosition start = default;
        LinePosition end = default;

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
                    start = ReadLinePosition( ref reader );

                    break;

                case "end":
                case "End":
                    end = ReadLinePosition( ref reader );

                    break;
            }
        }

        return new LinePositionSpan( start, end );
    }

    private static LinePosition ReadLinePosition( ref Utf8JsonReader reader )
    {
        if ( reader.TokenType != JsonTokenType.StartObject )
        {
            throw new JsonException( "Expected StartObject token for LinePosition." );
        }

        var line = 0;
        var character = 0;

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
                case "line":
                case "Line":
                    line = reader.GetInt32();

                    break;

                case "character":
                case "Character":
                    character = reader.GetInt32();

                    break;
            }
        }

        return new LinePosition( line, character );
    }

    public override void Write( Utf8JsonWriter writer, LinePositionSpan value, JsonSerializerOptions options )
    {
        writer.WriteStartObject();

        writer.WritePropertyName( "start" );
        WriteLinePosition( writer, value.Start );

        writer.WritePropertyName( "end" );
        WriteLinePosition( writer, value.End );

        writer.WriteEndObject();
    }

    private static void WriteLinePosition( Utf8JsonWriter writer, LinePosition value )
    {
        writer.WriteStartObject();
        writer.WriteNumber( "line", value.Line );
        writer.WriteNumber( "character", value.Character );
        writer.WriteEndObject();
    }
}

/// <summary>
/// System.Text.Json converter for nullable <see cref="LinePositionSpan"/>.
/// </summary>
internal sealed class NullableLinePositionSpanJsonConverter : JsonConverter<LinePositionSpan?>
{
    private static readonly LinePositionSpanJsonConverter _innerConverter = new();

    public override LinePositionSpan? Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
    {
        if ( reader.TokenType == JsonTokenType.Null )
        {
            return null;
        }

        return _innerConverter.Read( ref reader, typeof(LinePositionSpan), options );
    }

    public override void Write( Utf8JsonWriter writer, LinePositionSpan? value, JsonSerializerOptions options )
    {
        if ( value == null )
        {
            writer.WriteNullValue();
        }
        else
        {
            _innerConverter.Write( writer, value.Value, options );
        }
    }
}