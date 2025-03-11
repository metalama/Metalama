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

        // Add additional assemblies.
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

        return type;
    }
}