// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Backstage.Configuration;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Serialization;

/// <summary>
/// Provides serialization and deserialization for configuration files,
/// using System.Text.Json for writing and supporting both STJ and Newtonsoft.Json for reading (backward compatibility).
/// </summary>
internal static class ConfigurationFileSerializer
{
    /// <summary>
    /// Serializes a configuration file to JSON using System.Text.Json.
    /// </summary>
    public static string Serialize( ConfigurationFile configuration )
    {
        var type = configuration.GetType();
        var typeInfo = BackstageJsonContext.Indented.GetTypeInfo( type );

        if ( typeInfo != null )
        {
            return System.Text.Json.JsonSerializer.Serialize( configuration, typeInfo );
        }

        // Fallback for types not registered in the context (shouldn't happen in production)
        return System.Text.Json.JsonSerializer.Serialize( configuration, type, BackstageJsonContext.Indented.Options );
    }

    /// <summary>
    /// Deserializes a configuration file from JSON, trying System.Text.Json first and falling back to Newtonsoft.Json for backward compatibility.
    /// </summary>
    public static bool TryDeserialize<T>( string json, [NotNullWhen( true )] out T? result )
        where T : ConfigurationFile
    {
        if ( TryDeserialize( json, typeof(T), out var obj ) )
        {
            result = (T?) obj;

            return result != null;
        }

        result = null;

        return false;
    }

    /// <summary>
    /// Deserializes a configuration file from JSON, trying System.Text.Json first and falling back to Newtonsoft.Json for backward compatibility.
    /// </summary>
    public static bool TryDeserialize( string json, Type type, [NotNullWhen( true )] out ConfigurationFile? result )
    {
        // First, try System.Text.Json
        if ( TryDeserializeWithStj( json, type, out result ) )
        {
            return true;
        }

        // Fall back to Newtonsoft.Json for backward compatibility with existing files
        return TryDeserializeWithNewtonsoft( json, type, out result );
    }

    private static bool TryDeserializeWithStj( string json, Type type, [NotNullWhen( true )] out ConfigurationFile? result )
    {
        try
        {
            var typeInfo = BackstageJsonContext.Indented.GetTypeInfo( type );

            if ( typeInfo != null )
            {
                result = (ConfigurationFile?) System.Text.Json.JsonSerializer.Deserialize( json, typeInfo );

                return result != null;
            }

            // Fallback for types not registered in the context
            result = (ConfigurationFile?) System.Text.Json.JsonSerializer.Deserialize( json, type, BackstageJsonContext.Indented.Options );

            return result != null;
        }
        catch ( System.Text.Json.JsonException )
        {
            result = null;

            return false;
        }
    }

    private static bool TryDeserializeWithNewtonsoft( string json, Type type, [NotNullWhen( true )] out ConfigurationFile? result )
    {
        try
        {
            result = (ConfigurationFile?) JsonConvert.DeserializeObject( json, type );

            return result != null;
        }
        catch ( Newtonsoft.Json.JsonException )
        {
            result = null;

            return false;
        }
    }
}
