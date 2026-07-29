// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Metalama.DesignTime.HostSimulator;

/// <summary>
/// Loads analyzer assemblies into one <see cref="AssemblyLoadContext"/> per directory, which is how Roslyn loads
/// them on .NET.
/// </summary>
/// <remarks>
/// <para>
/// This reproduces <c>Microsoft.CodeAnalysis.AnalyzerAssemblyLoader.DirectoryLoadContext</c> (see
/// <c>src/Compilers/Core/Portable/DiagnosticAnalyzer/AnalyzerAssemblyLoader.Core.cs</c> in the Roslyn sources).
/// Roslyn's type is internal, so it cannot be reused, but the isolation rule it implements is the whole point of
/// this simulator: analyzers coming from two different NuGet package directories land in two different load
/// contexts, so a solution referencing two versions of Metalama really does load two copies of
/// <c>Metalama.Framework.Engine</c>, exactly as in an IDE.
/// </para>
/// <para>
/// Assemblies that the host itself provides (Roslyn, and anything already in the default context) are resolved from
/// the host rather than reloaded, which is what Roslyn's <c>CompilerResolver</c> does. Without this, the analyzer
/// would see a second copy of <see cref="Compilation"/> and fail to bind against the compilation we hand it.
/// </para>
/// </remarks>
internal sealed class IsolatedAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
{
    private readonly object _sync = new();
    private readonly Dictionary<string, DirectoryLoadContext> _contextsByDirectory = new( StringComparer.OrdinalIgnoreCase );
    private readonly Dictionary<string, string> _pathsBySimpleName = new( StringComparer.OrdinalIgnoreCase );
    private readonly AssemblyLoadContext _hostContext = AssemblyLoadContext.GetLoadContext( typeof(Compilation).Assembly )!;

    /// <summary>
    /// Gets the number of load contexts created so far, which is the number of distinct analyzer directories seen.
    /// </summary>
    public int LoadContextCount
    {
        get
        {
            lock ( this._sync )
            {
                return this._contextsByDirectory.Count;
            }
        }
    }

    public void AddDependencyLocation( string fullPath )
    {
        lock ( this._sync )
        {
            this._pathsBySimpleName[Path.GetFileNameWithoutExtension( fullPath )] = fullPath;
        }
    }

    public Assembly LoadFromPath( string fullPath )
    {
        var directory = Path.GetDirectoryName( Path.GetFullPath( fullPath ) )!;

        DirectoryLoadContext context;

        lock ( this._sync )
        {
            if ( !this._contextsByDirectory.TryGetValue( directory, out var existing ) )
            {
                existing = new DirectoryLoadContext( directory, this );
                this._contextsByDirectory.Add( directory, existing );
            }

            context = existing;
        }

        return context.LoadFromAssemblyPath( fullPath );
    }

    private bool TryGetDependencyPath( string simpleName, out string? path )
    {
        lock ( this._sync )
        {
            return this._pathsBySimpleName.TryGetValue( simpleName, out path );
        }
    }

    /// <summary>
    /// The load context of a single analyzer directory.
    /// </summary>
    private sealed class DirectoryLoadContext : AssemblyLoadContext
    {
        private readonly string _directory;
        private readonly IsolatedAnalyzerAssemblyLoader _loader;

        public DirectoryLoadContext( string directory, IsolatedAnalyzerAssemblyLoader loader ) : base( $"analyzers:{directory}" )
        {
            this._directory = directory;
            this._loader = loader;
        }

        protected override Assembly? Load( AssemblyName assemblyName )
        {
            if ( assemblyName.Name == null )
            {
                return null;
            }

            // The host always gets the first chance, so that Roslyn types are unified between the host and the
            // analyzer. This is what makes the compilation objects we pass to the analyzer usable by it.
            try
            {
                var hostAssembly = this._loader._hostContext.LoadFromAssemblyName( assemblyName );

                if ( hostAssembly != null )
                {
                    return hostAssembly;
                }
            }
            catch ( FileNotFoundException )
            {
                // Not provided by the host: fall through to the directory.
            }

            var localPath = Path.Combine( this._directory, assemblyName.Name + ".dll" );

            if ( File.Exists( localPath ) )
            {
                return this.LoadFromAssemblyPath( localPath );
            }

            if ( this._loader.TryGetDependencyPath( assemblyName.Name, out var registeredPath ) && File.Exists( registeredPath ) )
            {
                return this.LoadFromAssemblyPath( registeredPath! );
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll( string unmanagedDllName )
        {
            var path = Path.Combine( this._directory, unmanagedDllName );

            return File.Exists( path ) ? this.LoadUnmanagedDllFromPath( path ) : IntPtr.Zero;
        }
    }
}
