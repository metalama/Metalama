// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Text.Json;

namespace Metalama.Patterns.Caching.Serializers;

/// <summary>
/// An implementation of <see cref="ICachingSerializer"/> based on <see cref="JsonSerializer"/>.
/// </summary>
/// <remarks>
/// <para>This serializer stores the assembly-qualified type name along with the JSON payload,
/// allowing for polymorphic deserialization of cached values.</para>
/// <para>To customize type name resolution (for example, for type forwarding scenarios),
/// override the <see cref="GetTypeName"/> and <see cref="ResolveTypeName"/> methods.</para>
/// <para>A union of C# 15 is serialized by a converter of this class rather than by the default object
/// conversion of <see cref="JsonSerializer"/>. The <c>Value</c> property of a union has no setter, so the default
/// conversion writes the value of the current case and deserializes the default value of the union.</para>
/// </remarks>
/// <seealso cref="ICachingSerializer"/>
[PublicAPI]
public partial class JsonCachingSerializer : ICachingSerializer
{
    private const byte _nullMarker = 0;
    private const byte _objectMarker = 1;

    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonCachingSerializer"/> class.
    /// </summary>
    /// <param name="options">Optional <see cref="JsonSerializerOptions"/> to customize JSON serialization.
    /// If <c>null</c>, default options are used.</param>
    /// <remarks>
    /// The options given by the caller are copied, because this constructor adds the converter of the unions of
    /// C# 15 to them, and modifying the instance of the caller would change the behaviour of every other use of it.
    /// A modification that the caller makes to its own instance after this constructor returns has no effect on this
    /// serializer.
    /// </remarks>
    public JsonCachingSerializer( JsonSerializerOptions? options = null )
    {
        this._options = options == null ? new JsonSerializerOptions() : new JsonSerializerOptions( options );
        this._options.Converters.Add( new UnionJsonConverterFactory( this ) );
    }

    /// <inheritdoc />
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

    /// <summary>
    /// Gets the type name to store in the serialized data. Override to customize type name mapping.
    /// </summary>
    /// <param name="type">The type to get the name for.</param>
    /// <returns>The type name string to store. By default, returns the assembly-qualified name.</returns>
    protected virtual string GetTypeName( Type type ) => type.AssemblyQualifiedName!;

    /// <summary>
    /// Resolves a type name from the serialized data back to a <see cref="Type"/>. Override to customize type resolution.
    /// </summary>
    /// <param name="assemblyQualifiedTypeName">The type name from the serialized data.</param>
    /// <returns>The resolved <see cref="Type"/>.</returns>
    protected virtual Type ResolveTypeName( string assemblyQualifiedTypeName ) => Type.GetType( assemblyQualifiedTypeName, throwOnError: true )!;

    /// <inheritdoc />
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