// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;
using Newtonsoft.Json.Serialization;
using StreamJsonRpc.Protocol;
using System.Collections.Concurrent;
using System.Reflection;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// An implementation of <see cref="ISerializationBinder"/> that strips version numbers from non-Metalama assemblies.
/// </summary>
public sealed class JsonSerializationBinder : DefaultSerializationBinder
{
    private readonly ConcurrentDictionary<string, Assembly> _assemblies = new();
    private readonly Dictionary<string, string> _assemblyNames = new();
    private readonly JsonSerializationAllowList _allowList = new();

    /// <summary>
    /// Gets the per-type allow-list enforced by <see cref="BindToType"/> (security fix #1651 / GHSA-h26j-4vp7-x9w2).
    /// </summary>
    internal JsonSerializationAllowList AllowList => this._allowList;

    public JsonSerializationBinder( Action<JsonSerializationBinderConfiguration>? configure = null )
    {
        var configuration = new JsonSerializationBinderConfiguration( this );

        // Add system dependencies.
        configuration.AddAssemblyOfType( typeof(ImmutableArray<>) ); // System.Collections.Immutable
        configuration.AddAssemblyOfType( typeof(CommonErrorData) );  // StreamJsonRpc

        // Add the current assembly. Note that in VSX it is merged inside a different assembly named Metalama.Repacked.
        configuration.AddAssemblyOfType( typeof(ProjectKey), "Metalama.Framework.DesignTime.Rpc", "Metalama.Repacked" ); // The current assembly

        // Add system assemblies.
        configuration.AddSystemLibrary( "System.Private.CoreLib" );
        configuration.AddSystemLibrary( "mscorlib" );

        // SECURITY (#1651 / GHSA-h26j-4vp7-x9w2): in addition to the assembly-level checks above, BindToType enforces a
        // per-type allow-list. The wire uses TypeNameHandling.All, so without this an attacker who can write to the
        // design-time pipe could name any type in an allow-listed assembly (e.g. System.Private.CoreLib) and have it
        // instantiated before any application logic runs (an unsafe-deserialization / gadget-chain RCE primitive).
        // System / collection / protocol types that legitimately travel on the wire:
        configuration.AddType( typeof(ImmutableArray<>) );
        configuration.AddType( typeof(ImmutableDictionary<,>) );
        configuration.AddType( typeof(string) );
        configuration.AddType( typeof(CommonErrorData) );

        // RPC contract types defined in this assembly. Types from higher-level assemblies (Metalama.Framework.DesignTime,
        // .Engine and core Metalama.Framework) are registered by the IJsonSerializationBinderProvider via 'configure' below.
        configuration.AddType( typeof(ProjectKey) );
        configuration.AddType( typeof(RpcEventEnvelope) );
        configuration.AddType( typeof(RpcEventData) );
        configuration.AddType( typeof(Notifications.CompilationResultChangedEventData) );
        configuration.AddType( typeof(Notifications.EndpointChangedEventData) );

        // Add additional assemblies and allow-listed types.
        configure?.Invoke( configuration );
    }

    internal void TryAddAssembly( string assemblyName, Assembly assembly )
    {
        if ( this._assemblies.TryAdd( assemblyName, assembly ) )
        {
            this._assemblyNames.Add( assemblyName, assembly.FullName );
        }
    }

    public override Type BindToType( string? assemblyName, string typeName )
    {
        if ( !this._assemblies.TryGetValue( assemblyName, out var assembly ) )
        {
            assembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where( a => a.GetName().Name == assemblyName )
                .OrderByDescending( a => a.GetName().Version )
                .FirstOrDefault();

            if ( assembly == null )
            {
                throw new InvalidOperationException( $"The assembly '{assemblyName}' is not yet loaded in the AppDomain." );
            }

            this.TryAddAssembly( assemblyName, assembly );
        }

        var modifiedTypeName = JsonSerializationBinderHelper.QualifyAssemblies( typeName, this._assemblyNames );

        var type = assembly.GetType( modifiedTypeName );

        // SECURITY (#1651 / GHSA-h26j-4vp7-x9w2): the wire uses TypeNameHandling.All, so an attacker who can write to the
        // design-time named pipe can name an arbitrary loadable type and have it instantiated before any application logic
        // runs. Assembly-level allow-listing is insufficient because allow-listed assemblies (e.g. Metalama.Framework.Engine,
        // System.Private.CoreLib) contain dangerous types. Enforce the per-type allow-list here, the untrusted boundary.
        if ( type != null && !this._allowList.IsAllowed( type ) )
        {
            throw new InvalidOperationException(
                $"For security reasons, the type '{type.FullName}' from assembly '{assemblyName}' cannot be deserialized "
                + "over the design-time RPC channel because it is not on the RPC type allow-list. See issue #1651." );
        }

        return type;
    }
}