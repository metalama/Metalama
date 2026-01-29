// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metalama.Framework.Engine.Serialization;

/// <summary>
/// System.Text.Json converter for <see cref="LanguageVersion"/>.
/// We serialize the language version as an integer for cross-Roslyn-version compatibility.
/// </summary>
internal sealed class LanguageVersionJsonConverter : JsonConverter<LanguageVersion?>
{
    public override LanguageVersion? Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
    {
        if ( reader.TokenType == JsonTokenType.Null )
        {
            return null;
        }

        if ( reader.TokenType == JsonTokenType.Number )
        {
            return (LanguageVersion) reader.GetInt32();
        }

        throw new JsonException( $"Unexpected token type {reader.TokenType} for LanguageVersion." );
    }

    public override void Write( Utf8JsonWriter writer, LanguageVersion? value, JsonSerializerOptions options )
    {
        if ( value == null )
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteNumberValue( (int) value.Value );
        }
    }
}
