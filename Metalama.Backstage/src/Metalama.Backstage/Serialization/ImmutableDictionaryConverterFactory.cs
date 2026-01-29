// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
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
