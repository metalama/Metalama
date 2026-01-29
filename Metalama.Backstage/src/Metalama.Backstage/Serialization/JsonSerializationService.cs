// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Metalama.Backstage.Serialization;

/// <summary>
/// Implementation of <see cref="IJsonSerializationService"/> that uses source-generated JSON contexts
/// combined via <see cref="JsonSerializerOptions.TypeInfoResolverChain"/>.
/// </summary>
internal sealed class JsonSerializationService : IJsonSerializationService
{
    private readonly JsonSerializerOptions _options;

    public JsonSerializationService( IEnumerable<IJsonTypeInfoResolver> additionalResolvers )
    {
        this._options = BackstageJsonContext.CreateCombinedOptions( writeIndented: true, additionalResolvers );
    }

    /// <inheritdoc />
    public string Serialize<T>( T value )
    {
        // GetTypeInfo throws NotSupportedException if type is not found in any resolver
        var typeInfo = this._options.GetTypeInfo( typeof(T) );

        return JsonSerializer.Serialize( value, typeInfo );
    }

    /// <inheritdoc />
    public string Serialize( object value, Type type )
    {
        // GetTypeInfo throws NotSupportedException if type is not found in any resolver
        var typeInfo = this._options.GetTypeInfo( type );

        return JsonSerializer.Serialize( value, typeInfo );
    }

    /// <inheritdoc />
    public bool TryDeserialize<T>( string json, [NotNullWhen( true )] out T? result )
    {
        if ( this.TryDeserialize( json, typeof(T), out var obj ) )
        {
            result = (T?) obj;

            return result != null;
        }

        result = default;

        return false;
    }

    /// <inheritdoc />
    public bool TryDeserialize( string json, Type type, [NotNullWhen( true )] out object? result )
    {
        try
        {
            // GetTypeInfo throws NotSupportedException if type is not found in any resolver
            var typeInfo = this._options.GetTypeInfo( type );

            result = JsonSerializer.Deserialize( json, typeInfo );

            return result != null;
        }
        catch ( JsonException )
        {
            result = null;

            return false;
        }
    }
}
