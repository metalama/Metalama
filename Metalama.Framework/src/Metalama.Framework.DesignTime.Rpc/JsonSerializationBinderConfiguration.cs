// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Reflection;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// Provides a fluent API for configuring <see cref="JsonSerializationBinder"/> with assemblies
/// that should use version-agnostic serialization. This allows different versions of assemblies
/// to coexist in the same AppDomain while maintaining JSON serialization compatibility.
/// </summary>
public sealed class JsonSerializationBinderConfiguration
{
    private readonly JsonSerializationBinder _binder;

    internal JsonSerializationBinderConfiguration( JsonSerializationBinder binder )
    {
        this._binder = binder;
    }

    /// <summary>
    /// Registers an assembly by providing a type from that assembly. The assembly will be
    /// registered under its actual name and any alternate names provided.
    /// </summary>
    /// <param name="t">A type from the assembly to register.</param>
    /// <param name="alternateNames">Optional alternate names to register the assembly under
    /// (e.g., for merged assemblies like "Metalama.Repacked").</param>
    public void AddAssemblyOfType( Type t, params string[] alternateNames )
    {
        var assemblyName = t.Assembly.GetName().Name;
        this._binder.TryAddAssembly( assemblyName, t.Assembly );

        foreach ( var name in alternateNames )
        {
            if ( name != assemblyName )
            {
                this._binder.TryAddAssembly( name, t.Assembly );
            }
        }
    }

    internal void AddSystemLibrary( string name )
    {
        this._binder.TryAddAssembly( name, typeof(int).Assembly );
    }

    /// <summary>
    /// Registers an assembly by name, using the same version as another type's assembly.
    /// This is useful for registering related assemblies that should share version information.
    /// </summary>
    /// <param name="t">A type whose assembly version should be matched.</param>
    /// <param name="assemblyName">The name of the assembly to register.</param>
    public void AddAssemblyWithSameVersionThanType( Type t, string assemblyName )
    {
        var newAssemblyName = new AssemblyName( t.Assembly.FullName.Replace( t.Assembly.GetName().Name, assemblyName ) );

        var assembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(
                a =>
                {
                    var name = a.GetName();

                    return AssemblyName.ReferenceMatchesDefinition( newAssemblyName, name ) && name.Version == newAssemblyName.Version;
                } );

        if ( assembly == null )
        {
            assembly = Assembly.Load( newAssemblyName );
        }

        this._binder.TryAddAssembly( assemblyName, assembly );
    }

    /// <summary>
    /// Adds a type to the RPC deserialization allow-list (#1651). Open generic definitions (e.g.
    /// <c>typeof(ImmutableArray&lt;&gt;)</c>) are recorded as generic definitions; their type arguments are validated
    /// recursively at deserialization time.
    /// </summary>
    public void AddType( Type type ) => this._binder.AllowList.Add( type );

    /// <summary>
    /// Adds a type to the RPC deserialization allow-list (#1651) by full name, <em>without loading it</em>. This is the
    /// preferred overload for contract DTO types that implement a shared <c>Metalama.Framework.DesignTime.Contracts</c>
    /// interface: loading such a type eagerly (via <c>typeof</c>) crashes in the multi-version design-time scenario when a
    /// different version of the Contracts assembly is loaded in the process (issue #31075). The allow-list matches by full
    /// name only, so the assembly is irrelevant.
    /// </summary>
    public void AddType( string fullName, bool isGenericDefinition = false )
        => this._binder.AllowList.Add( fullName, isGenericDefinition );
}