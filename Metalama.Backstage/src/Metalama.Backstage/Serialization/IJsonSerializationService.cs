// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Serialization;

/// <summary>
/// Provides JSON serialization and deserialization services using System.Text.Json with source-generated contexts.
/// </summary>
[PublicAPI]
public interface IJsonSerializationService : IBackstageService
{
    /// <summary>
    /// Serializes an object to JSON.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <returns>The JSON string.</returns>
    string Serialize<T>( T value );

    /// <summary>
    /// Serializes an object to JSON using runtime type information.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <param name="type">The type to use for serialization.</param>
    /// <returns>The JSON string.</returns>
    string Serialize( object value, Type type );

    /// <summary>
    /// Deserializes a JSON string to an object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="json">The JSON string.</param>
    /// <param name="result">The deserialized object, or null if deserialization failed.</param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
    bool TryDeserialize<T>( string json, [NotNullWhen( true )] out T? result );

    /// <summary>
    /// Deserializes a JSON string to an object using runtime type information.
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <param name="type">The type to deserialize to.</param>
    /// <param name="result">The deserialized object, or null if deserialization failed.</param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
    bool TryDeserialize( string json, Type type, [NotNullWhen( true )] out object? result );
}