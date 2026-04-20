// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Metalama.Framework.Engine.Serialization;

/// <summary>
/// Provides serialization and deserialization for manifest files using System.Text.Json.
/// </summary>
internal static class ManifestSerializer
{
    /// <summary>
    /// Serializes a manifest to JSON using System.Text.Json.
    /// </summary>
    public static string Serialize<T>( T manifest )
    {
        return JsonSerializer.Serialize( manifest, ManifestJsonContext.Indented.Options );
    }

    /// <summary>
    /// Deserializes a manifest from JSON using System.Text.Json.
    /// </summary>
    public static T Deserialize<T>( string json )
    {
        if ( TryDeserialize<T>( json, out var result ) )
        {
            return result;
        }

        throw new JsonException( $"Failed to deserialize {typeof(T).Name} from JSON." );
    }

    /// <summary>
    /// Deserializes a manifest from JSON using System.Text.Json.
    /// </summary>
    public static bool TryDeserialize<T>( string json, [NotNullWhen( true )] out T? result )
    {
        try
        {
            result = JsonSerializer.Deserialize<T>( json, ManifestJsonContext.Indented.Options );

            return result != null;
        }
        catch ( JsonException )
        {
            result = default;

            return false;
        }
    }
}