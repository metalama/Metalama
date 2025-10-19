// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using System;

namespace Metalama.Framework.Engine.CompileTime.Manifest;

internal sealed class LanguageVersionConverter : JsonConverter<LanguageVersion?>
{
    public override void WriteJson( JsonWriter writer, LanguageVersion? value, JsonSerializer serializer ) => writer.WriteValue( (int?) value );

    public override LanguageVersion? ReadJson(
        JsonReader reader,
        Type objectType,
        LanguageVersion? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer )
        => (LanguageVersion?) (long?) reader.Value;
}