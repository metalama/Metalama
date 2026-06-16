// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using MessagePack;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// A <see cref="MessagePackSerializerOptions"/> that enforces a closed type allow-list on the <em>typeless</em>
/// deserialization path used by the design-time RPC channel. Only types marked with <see cref="RpcContractAttribute"/>
/// may be resolved from a type name embedded on the wire.
/// </summary>
/// <remarks>
/// <para>
/// The RPC wire format uses <c>TypelessObjectResolver</c> to support abstract-typed members (notably
/// <see cref="RpcEventEnvelope.Data"/>, typed as the abstract <see cref="RpcEventData"/>). The typeless resolver embeds
/// the CLR type name of the concrete value on the wire and reconstructs it via <see cref="MessagePackSerializerOptions.LoadType"/>
/// on deserialization — the MessagePack analogue of <c>BinaryFormatter</c> / <c>TypeNameHandling.All</c>. Without a
/// restriction, an attacker able to write a crafted message to the pipe could name an arbitrary type and have it
/// instantiated before any application logic runs (an unsafe-deserialization / gadget-chain primitive). See issue #1651.
/// </para>
/// <para>
/// By overriding <see cref="LoadType"/> we reject any type that is not an approved <see cref="RpcContractAttribute"/>,
/// removing the arbitrary-type primitive while preserving the legitimate polymorphism the protocol relies on. The check
/// happens before the object is constructed. Concrete-typed members (which never take the typeless path and therefore
/// never call <see cref="LoadType"/>) are unaffected.
/// </para>
/// <para>
/// A small, explicit allow-list of data-only system types (see <c>_allowedSystemTypes</c>) is also accepted: these
/// types legitimately ride the typeless path but cannot carry <see cref="RpcContractAttribute"/> because we do not
/// own them (e.g. <c>Microsoft.CodeAnalysis.SymbolKey</c>, embedded in <c>SymbolId</c>).
/// </para>
/// </remarks>
internal sealed class RpcContractMessagePackOptions : MessagePackSerializerOptions
{
    // System types that legitimately travel on the wire but cannot carry [RpcContract] because we do not own them.
    // These are data-only types (no behavior is invoked on deserialization), pinned by both full name and declaring
    // assembly so a same-named gadget from another assembly cannot slip through. Keep this list as small as possible.
    //
    // Microsoft.CodeAnalysis.SymbolKey: an opaque, string-backed Roslyn handle. SymbolId (see SerializableIds.SymbolId)
    // stores a boxed SymbolKey in an object-typed field, so the typeless resolver writes its CLR type name on the wire.
    // The type is internal to Microsoft.CodeAnalysis.Workspaces, so it can only be allow-listed by name.
    private static readonly HashSet<(string FullName, string AssemblyName)> _allowedSystemTypes =
        new() { ("Microsoft.CodeAnalysis.SymbolKey", "Microsoft.CodeAnalysis.Workspaces") };

    private readonly ConcurrentDictionary<Type, bool> _isAllowedCache = new();

    public RpcContractMessagePackOptions( MessagePackSerializerOptions copyFrom ) : base( copyFrom ) { }

    // StreamJsonRpc's MessagePackFormatter massages the user-supplied options (e.g. WithResolver) before use, which
    // calls Clone(). The base implementation throws unless a derived type overrides Clone, so we must propagate our
    // own type here — this also ensures the LoadType allow-list survives StreamJsonRpc's option massaging and thus
    // applies to the real RPC dispatch path, not only to direct MessagePackHelper usage.
    protected override MessagePackSerializerOptions Clone() => new RpcContractMessagePackOptions( this );

    public override Type LoadType( string typeName )
    {
        var type = base.LoadType( typeName );

        if ( type != null && !this.IsAllowed( type ) )
        {
            throw new MessagePackSerializationException(
                $"For security reasons, the type '{type.FullName}' cannot be deserialized over the design-time RPC channel "
                + $"because it is not marked with [{nameof(RpcContractAttribute)}]." );
        }

        return type;
    }

    private bool IsAllowed( Type type )
        => this._isAllowedCache.GetOrAdd(
            type,
            static t => t.GetCustomAttribute<RpcContractAttribute>( inherit: false ) != null
                        || (t.FullName != null && _allowedSystemTypes.Contains( (t.FullName, t.Assembly.GetName().Name!) )) );
}
