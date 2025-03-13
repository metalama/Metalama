// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Serializers;

/// <summary>
/// Serializes an object into a byte array and deserializes the byte array back.
/// </summary>
public interface ICachingSerializer
{
    /// <summary>
    /// Serializes an object into a byte array.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <param name="writer"></param>
    void Serialize( object? value, BinaryWriter writer );

    /// <summary>
    /// Deserializes a byte array into an object.
    /// </summary>
    /// <param name="reader"></param>
    object? Deserialize( BinaryReader reader );
}