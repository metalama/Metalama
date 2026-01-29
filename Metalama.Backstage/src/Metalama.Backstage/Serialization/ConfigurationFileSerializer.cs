// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Metalama.Backstage.Configuration;

namespace Metalama.Backstage.Serialization;

/// <summary>
/// Provides serialization and deserialization for configuration files using System.Text.Json.
/// </summary>
internal static class ConfigurationFileSerializer
{
    /// <summary>
    /// Serializes a configuration file to JSON.
    /// </summary>
    public static string Serialize( ConfigurationFile configuration )
    {
        var type = configuration.GetType();
        var typeInfo = BackstageJsonContext.Indented.GetTypeInfo( type );

        if ( typeInfo != null )
        {
            return JsonSerializer.Serialize( configuration, typeInfo );
        }

        // Fallback for types not registered in the context (shouldn't happen in production)
        return JsonSerializer.Serialize( configuration, type, BackstageJsonContext.Indented.Options );
    }

    /// <summary>
    /// Deserializes a configuration file from JSON.
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
    /// Deserializes a configuration file from JSON.
    /// </summary>
    public static bool TryDeserialize( string json, Type type, [NotNullWhen( true )] out ConfigurationFile? result )
    {
        try
        {
            var typeInfo = BackstageJsonContext.Indented.GetTypeInfo( type );

            if ( typeInfo != null )
            {
                result = (ConfigurationFile?) JsonSerializer.Deserialize( json, typeInfo );

                return result != null;
            }

            // Fallback for types not registered in the context
            result = (ConfigurationFile?) JsonSerializer.Deserialize( json, type, BackstageJsonContext.Indented.Options );

            return result != null;
        }
        catch ( JsonException )
        {
            result = null;

            return false;
        }
    }
}
