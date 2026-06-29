// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Concurrent;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// A closed, per-type allow-list of CLR types that may be deserialized over the design-time RPC channel.
/// See issue #1651 (GHSA-h26j-4vp7-x9w2): the JSON wire format uses <see cref="Newtonsoft.Json.TypeNameHandling.All"/>,
/// which embeds a CLR type name on the wire for every non-primitive object and reconstructs it on read. Without a
/// per-type restriction, any type in an allow-listed assembly (including <c>System.Private.CoreLib</c>) could be
/// instantiated before any application logic runs — an unsafe-deserialization / gadget-chain RCE primitive, the
/// Newtonsoft analogue of <c>BinaryFormatter</c>. The check is enforced by <see cref="JsonSerializationBinder.BindToType"/>,
/// the untrusted deserialization boundary.
/// </summary>
internal sealed class JsonSerializationAllowList
{
    // Keyed by (assembly simple name, type full name). Pinning by assembly name as well as full name prevents a
    // same-named gadget from a different assembly from slipping through. The key always uses the *resolved* type's
    // assembly, which is the runtime that will actually instantiate the object.
    private readonly HashSet<(string AssemblyName, string FullName)> _allowedTypes = new();

    // Open generic definitions that may appear on the wire (e.g. ImmutableArray<>); their type arguments are
    // validated separately and recursively.
    private readonly HashSet<(string AssemblyName, string FullName)> _allowedGenericDefinitions = new();

    private readonly ConcurrentDictionary<Type, bool> _cache = new();

    /// <summary>
    /// Adds a type to the allow-list. Open generic definitions (e.g. <c>typeof(ImmutableArray&lt;&gt;)</c>) are recorded
    /// as generic definitions. Prefer this overload for any type we can reference, so the key uses the resolved
    /// assembly identity (important under the <c>Metalama.Repacked</c> ILMerge in the VS extension).
    /// </summary>
    public void Add( Type type )
    {
        var key = GetKey( type );

        if ( type.IsGenericTypeDefinition )
        {
            this._allowedGenericDefinitions.Add( key );
        }
        else
        {
            this._allowedTypes.Add( key );
        }
    }

    /// <summary>
    /// Adds a type to the allow-list by name, for types that cannot be referenced at compile time (e.g. types owned by
    /// another assembly we do not depend on). Use <see cref="Add(System.Type)"/> whenever the type can be referenced.
    /// </summary>
    public void Add( string assemblyName, string fullName, bool isGenericDefinition = false )
    {
        if ( isGenericDefinition )
        {
            this._allowedGenericDefinitions.Add( (assemblyName, fullName) );
        }
        else
        {
            this._allowedTypes.Add( (assemblyName, fullName) );
        }
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
            var definition = type.GetGenericTypeDefinition();

            if ( !this._allowedGenericDefinitions.Contains( GetKey( definition ) ) )
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

        return this._allowedTypes.Contains( GetKey( type ) );
    }

    private static (string AssemblyName, string FullName) GetKey( Type type )
        => (type.Assembly.GetName().Name ?? "", type.FullName ?? type.Name);
}
