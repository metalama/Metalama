// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Reflection;

namespace Metalama.Framework.DesignTime.Rpc;

public sealed class JsonSerializationBinderConfiguration
{
    private readonly JsonSerializationBinder _binder;

    internal JsonSerializationBinderConfiguration( JsonSerializationBinder binder )
    {
        this._binder = binder;
    }

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
}