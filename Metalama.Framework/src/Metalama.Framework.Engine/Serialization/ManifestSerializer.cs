// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.Serialization;

/// <summary>
/// Provides serialization and deserialization for manifest files,
/// using System.Text.Json for writing and supporting both STJ and Newtonsoft.Json for reading (backward compatibility).
/// </summary>
internal static class ManifestSerializer
{
    private static readonly Newtonsoft.Json.JsonConverter[] _newtonsoftConverters =
        [new StringEnumConverter(), new CompileTime.Manifest.LanguageVersionConverter()];

    /// <summary>
    /// Serializes a manifest to JSON using System.Text.Json.
    /// </summary>
    public static string Serialize<T>( T manifest )
    {
        return System.Text.Json.JsonSerializer.Serialize( manifest, ManifestJsonContext.Indented.Options );
    }

    /// <summary>
    /// Deserializes a manifest from JSON, trying System.Text.Json first and falling back to Newtonsoft.Json for backward compatibility.
    /// </summary>
    public static T Deserialize<T>( string json )
    {
        if ( TryDeserialize<T>( json, out var result ) )
        {
            return result;
        }

        throw new System.Text.Json.JsonException( $"Failed to deserialize {typeof(T).Name} from JSON." );
    }

    /// <summary>
    /// Deserializes a manifest from JSON, trying System.Text.Json first and falling back to Newtonsoft.Json for backward compatibility.
    /// </summary>
    public static bool TryDeserialize<T>( string json, [NotNullWhen( true )] out T? result )
    {
        // First, try System.Text.Json
        if ( TryDeserializeWithStj( json, out result ) )
        {
            return true;
        }

        // Fall back to Newtonsoft.Json for backward compatibility with existing manifests
        return TryDeserializeWithNewtonsoft( json, out result );
    }

    private static bool TryDeserializeWithStj<T>( string json, [NotNullWhen( true )] out T? result )
    {
        try
        {
            result = System.Text.Json.JsonSerializer.Deserialize<T>( json, ManifestJsonContext.Indented.Options );

            return result != null;
        }
        catch ( System.Text.Json.JsonException )
        {
            result = default;

            return false;
        }
    }

    private static bool TryDeserializeWithNewtonsoft<T>( string json, [NotNullWhen( true )] out T? result )
    {
        try
        {
            result = JsonConvert.DeserializeObject<T>( json, _newtonsoftConverters );

            return result != null;
        }
        catch ( Newtonsoft.Json.JsonException )
        {
            result = default;

            return false;
        }
    }
}
