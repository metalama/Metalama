// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System.Text.Json;

namespace Metalama.Patterns.Caching.Serializers;

/// <summary>
/// A serialized based on <c>System.Text.Json</c>.
/// </summary>
[PublicAPI]
public class JsonCachingSerializer : ICachingSerializer
{
    private const byte _nullMarker = 0;
    private const byte _objectMarker = 1;

    private readonly JsonSerializerOptions _options;

    public JsonCachingSerializer( JsonSerializerOptions? options = null )
    {
        this._options = options ?? new JsonSerializerOptions();
    }

    public void Serialize( object? value, BinaryWriter writer )
    {
        if ( value == null )
        {
            writer.Write( _nullMarker );
        }
        else
        {
            writer.Write( _objectMarker );

            // Write the assembly-qualified name.
            writer.Write( this.GetTypeName( value.GetType() ) );

            // Write the JSON.
            var json = JsonSerializer.Serialize( value, this._options );
            writer.Write( json );
        }
    }

    protected virtual string GetTypeName( Type type ) => type.AssemblyQualifiedName!;

    protected virtual Type ResolveTypeName( string assemblyQualifiedTypeName ) => Type.GetType( assemblyQualifiedTypeName, throwOnError: true )!;

    public object? Deserialize( BinaryReader reader )
    {
        switch ( reader.ReadByte() )
        {
            case _nullMarker:
                return null;

            case _objectMarker:
                var typeName = reader.ReadString();
                var type = this.ResolveTypeName( typeName );

                // Read the JSON payload.
                var json = reader.ReadString();

                return JsonSerializer.Deserialize( json, type, this._options );

            default:
                throw new InvalidCacheItemException();
        }
    }
}