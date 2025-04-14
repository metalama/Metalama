// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Metalama.Framework.Engine.Utilities.AssemblyLoaders;

internal class NetFrameworkAssemblyLoader : AssemblyLoader
{
    private readonly Func<string, Assembly?> _resolveAssembly;

    public NetFrameworkAssemblyLoader(
        Func<string, Assembly?> resolveAssembly,
        string? debugName = null ) : base( debugName )
    {
        AppDomain.CurrentDomain.AssemblyResolve += this.OnAssemblyResolve;
        this._resolveAssembly = resolveAssembly;
    }

    private Assembly? OnAssemblyResolve( object? sender, ResolveEventArgs args ) => this._resolveAssembly( args.Name );

    public override Assembly LoadFromPath( string assemblyPath ) => Assembly.LoadFile( assemblyPath );

    public override Assembly LoadFromStream( Stream peStream, Stream? pdbStream )
    {
        return Assembly.Load( ReadBytes( peStream )!, ReadBytes( pdbStream ) );

        static byte[]? ReadBytes( Stream? stream )
        {
            if ( stream == null )
            {
                return null;
            }
            else
            {
                var memoryStream = new MemoryStream();
                stream.CopyTo( memoryStream );

                return memoryStream.ToArray();
            }
        }
    }

    public override bool IsCollectible( Assembly assembly ) => false;

    protected override IEnumerable<Assembly> GetAssemblies() => AppDomain.CurrentDomain.GetAssemblies();

    public override void Dispose()
    {
        base.Dispose();

        AppDomain.CurrentDomain.AssemblyResolve -= this.OnAssemblyResolve;
    }
}