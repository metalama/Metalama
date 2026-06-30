// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// A closed allow-list of CLR types, identified by full name, that may be deserialized over the design-time RPC channel.
/// See issue #1651 (GHSA-h26j-4vp7-x9w2): the JSON wire format uses <see cref="Newtonsoft.Json.TypeNameHandling.All"/>,
/// which embeds a CLR type name on the wire for every non-primitive object and reconstructs it on read. Without a
/// per-type restriction, any resolvable type (including <c>System.Private.CoreLib</c> gadgets) could be instantiated
/// before any application logic runs — an unsafe-deserialization / gadget-chain RCE primitive, the Newtonsoft analogue
/// of <c>BinaryFormatter</c>. The check is enforced by <see cref="JsonSerializationBinder.BindToType"/>, the untrusted
/// deserialization boundary.
/// </summary>
/// <remarks>
/// <para>
/// Types are matched by <see cref="System.Type.FullName"/> only, NOT by assembly identity. This is required by the
/// multi-version design-time scenario (issue #31075): the same logical contract type is shipped in version-suffixed
/// assemblies (e.g. <c>Metalama.Framework.DesignTime.4.8.0</c> vs <c>4.12.0</c>) and is repacked into
/// <c>Metalama.Repacked</c> in the VS extension, so its assembly identity differs between the communicating processes
/// while its full name is stable. Matching by full name also lets the allow-list be populated <em>without loading</em>
/// the contract types — see <see cref="Add(string,bool)"/>. Loading them eagerly (e.g. via <c>typeof</c>) crashes in the
/// multi-version scenario, because a contract type that implements a shared <c>Metalama.Framework.DesignTime.Contracts</c>
/// interface fails to load when a different version of that Contracts assembly is already loaded in the process.
/// </para>
/// <para>
/// Security is preserved because only the small, fixed set of contract full names is accepted: realistic deserialization
/// gadgets (whose full names are never one of our contracts) are rejected, as are all unregistered types. Generic type
/// arguments and array elements are validated recursively; scalar primitives and enums are always allowed.
/// </para>
/// </remarks>
internal sealed class JsonSerializationAllowList
{
    private readonly HashSet<string> _allowedTypeNames = new( StringComparer.Ordinal );

    // Full names of open generic definitions that may appear on the wire (e.g. "System.Collections.Immutable.ImmutableArray`1");
    // their type arguments are validated separately and recursively.
    private readonly HashSet<string> _allowedGenericDefinitionNames = new( StringComparer.Ordinal );

    private readonly ConcurrentDictionary<Type, bool> _cache = new();

    /// <summary>
    /// Adds a type to the allow-list, by full name. Open generic definitions (e.g. <c>typeof(ImmutableArray&lt;&gt;)</c>)
    /// are recorded as generic definitions. This overload loads the type, so use it only for types that are always safe to
    /// load in any process (e.g. the framework's own RPC types and BCL types). For contract types that implement shared
    /// <c>Metalama.Framework.DesignTime.Contracts</c> interfaces, use <see cref="Add(string,bool)"/> to avoid loading them.
    /// </summary>
    public void Add( Type type )
    {
        if ( type.IsGenericTypeDefinition )
        {
            this._allowedGenericDefinitionNames.Add( GetName( type ) );
        }
        else
        {
            this._allowedTypeNames.Add( GetName( type ) );
        }

        // A type registered here may already have a cached 'false' decision (its own, or that of an array/generic that
        // references it as an element/argument), e.g. if it was deserialized before an extension registered it through
        // the late-registration seam. Drop the cache so those decisions are recomputed against the updated allow-list.
        this._cache.Clear();
    }

    /// <summary>
    /// Adds a type to the allow-list by full name, <em>without loading it</em>. This is the preferred overload for contract
    /// DTO types: it avoids the eager type-loading that crashes in the multi-version design-time scenario (see the remarks
    /// on <see cref="JsonSerializationAllowList"/>).
    /// </summary>
    public void Add( string fullName, bool isGenericDefinition = false )
    {
        if ( isGenericDefinition )
        {
            this._allowedGenericDefinitionNames.Add( fullName );
        }
        else
        {
            this._allowedTypeNames.Add( fullName );
        }

        // See Add(Type): invalidate cached decisions that may predate this registration.
        this._cache.Clear();
    }

    public bool IsAllowed( Type type ) => this._cache.GetOrAdd( type, this.IsAllowedCore );

    private bool IsAllowedCore( Type type )
    {
        // Arrays: T[] is allowed iff the element type is allowed.
        if ( type.IsArray )
        {
            var elementType = type.GetElementType();

            return elementType != null && this.IsAllowed( elementType );
        }

        // Constructed generics: the open definition must be allow-listed AND every type argument must be allowed.
        // This is necessary as well as sufficient: for an empty ImmutableArray<Gadget> the element is never resolved
        // independently, so a smuggled type argument can only be caught here.
        if ( type.IsGenericType && !type.IsGenericTypeDefinition )
        {
            if ( !this._allowedGenericDefinitionNames.Contains( GetName( type.GetGenericTypeDefinition() ) ) )
            {
                return false;
            }

            foreach ( var argument in type.GetGenericArguments() )
            {
                if ( !this.IsAllowed( argument ) )
                {
                    return false;
                }
            }

            return true;
        }

        // Primitive scalar types (int, bool, long, double, char, ...) and enums carry no behavior on deserialization and
        // can never be a gadget, so they are always allowed (this also covers primitive/enum array elements and type
        // arguments). Nullable<T> arrives as a constructed generic and is handled above. Dangerous reference types and
        // unrecognized value types (e.g. System.Type) still require explicit registration.
        if ( type.IsPrimitive || type.IsEnum )
        {
            return true;
        }

        return this._allowedTypeNames.Contains( GetName( type ) );
    }

    private static string GetName( Type type ) => type.FullName ?? type.Name;
}
