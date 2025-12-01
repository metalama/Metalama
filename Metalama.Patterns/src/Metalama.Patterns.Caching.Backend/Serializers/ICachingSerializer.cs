// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Serializers;

/// <summary>
/// Serializes and deserializes cached objects for storage in backends that require binary serialization.
/// </summary>
/// <remarks>
/// <para>Serializers are typically used by distributed caching backends (such as Redis) to convert
/// cached objects to and from a binary format for network transmission and storage.</para>
/// <para>A default JSON serializer is available through <see cref="JsonCachingSerializer"/>.</para>
/// </remarks>
/// <seealso cref="JsonCachingSerializer"/>
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