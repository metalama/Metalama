// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Metalama.DesignTime.Hosting;

/// <summary>
/// Loads analyzer assemblies into the current application domain, which is how Visual Studio loads them on .NET Framework.
/// </summary>
/// <remarks>
/// <para>
/// .NET Framework has no assembly load context, so assemblies of two analyzer directories cannot be isolated from each other.
/// Visual Studio loads analyzers with <see cref="Assembly.LoadFrom(string)"/> as well, after it copies them to a shadow
/// directory. This loader does not copy them.
/// </para>
/// <para>
/// A dependency that the runtime cannot bind is resolved by its simple name from the paths passed to
/// <see cref="AddDependencyLocation"/>, through the <see cref="AppDomain.AssemblyResolve"/> event. The handler is installed
/// when the loader is created, and it remains installed for the lifetime of the process.
/// </para>
/// </remarks>
internal sealed class DesktopAnalyzerAssemblyLoader : HostAnalyzerAssemblyLoader
{
    private readonly object _sync = new();
    private readonly Dictionary<string, string> _pathsBySimpleName = new( StringComparer.OrdinalIgnoreCase );

    public DesktopAnalyzerAssemblyLoader()
    {
        AppDomain.CurrentDomain.AssemblyResolve += this.OnAssemblyResolve;
    }

    public override int LoadContextCount => 0;

    public override void AddDependencyLocation( string fullPath )
    {
        lock ( this._sync )
        {
            this._pathsBySimpleName[Path.GetFileNameWithoutExtension( fullPath )] = fullPath;
        }
    }

    public override Assembly LoadFromPath( string fullPath ) => Assembly.LoadFrom( fullPath );

    private Assembly? OnAssemblyResolve( object? sender, ResolveEventArgs args )
    {
        var simpleName = new AssemblyName( args.Name ).Name;

        if ( simpleName == null )
        {
            return null;
        }

        string? path;

        lock ( this._sync )
        {
            if ( !this._pathsBySimpleName.TryGetValue( simpleName, out path ) )
            {
                return null;
            }
        }

        return File.Exists( path ) ? Assembly.LoadFrom( path ) : null;
    }
}
#endif
