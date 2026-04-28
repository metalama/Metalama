// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using MessagePack;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// MessagePack adapter that uses the same serializer options as the production RPC layer
/// (see <see cref="BaseEndpoint"/>'s <c>CreateRpc</c>). Production code paths and cross-repo tests use this to
/// validate RPC contract types against the real wire path.
/// </summary>
/// <remarks>
/// The non-generic <see cref="Serialize(object, Type)"/> / <see cref="Deserialize(byte[], Type)"/> overloads
/// exist for callers that resolve the static type at runtime (e.g., a descriptor-based dispatch where the
/// concrete type is selected by an out-of-band identifier — see Premium's <c>CodeActionDescriptor</c> path).
/// </remarks>
public sealed class MessagePackHelper
{
    public byte[] Serialize<T>( T value ) => MessagePackSerializer.Serialize( value, BaseEndpoint.MessagePackOptions );

    public T Deserialize<T>( byte[] bytes ) => MessagePackSerializer.Deserialize<T>( bytes, BaseEndpoint.MessagePackOptions );

    public byte[] Serialize( object? value, Type type ) => MessagePackSerializer.Serialize( type, value, BaseEndpoint.MessagePackOptions );

    public object? Deserialize( byte[] bytes, Type type ) => MessagePackSerializer.Deserialize( type, bytes, BaseEndpoint.MessagePackOptions );
}
