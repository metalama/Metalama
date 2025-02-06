// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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