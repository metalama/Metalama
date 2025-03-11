// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.Serialization.Formatters.Binary;

namespace Metalama.Patterns.Caching.Serializers;

/// <summary>
/// An implementation of <see cref="BinaryFormatter"/> that uses <see cref="BinaryFormatter"/>
/// (for classes annotated with <see cref="SerializableAttribute"/>).
/// </summary>
[Obsolete( "The BinaryFormatter facility is considered obsolete." )]
public sealed class BinaryCachingSerializer : ICachingSerializer
{
    private const byte _null = 0;
    private const byte _object = 1;

    private readonly BinaryFormatter _serializer = new();

    /// <inheritdoc />
    public void Serialize( object? value, BinaryWriter writer )
    {
        if ( value == null )
        {
            writer.Write( _null );
        }
        else
        {
            writer.Write( _object );
            this._serializer.Serialize( writer.BaseStream, value );
        }
    }

    /// <inheritdoc />  
    public object? Deserialize( BinaryReader reader )
    {
        switch ( reader.ReadByte() )
        {
            case _null:
                return null;

            case _object:
                return this._serializer.Deserialize( reader.BaseStream );

            default:
                throw new InvalidCacheItemException();
        }
    }
}