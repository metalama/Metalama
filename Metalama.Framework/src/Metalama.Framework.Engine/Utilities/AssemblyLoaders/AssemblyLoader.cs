// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Metalama.Framework.Engine.Utilities.AssemblyLoaders;

internal abstract class AssemblyLoader : IDisposable
{
    public string DebugName { get; }

    private static readonly Version _defaultVersion = new();

    protected AssemblyLoader( string? debugName )
    {
        this.DebugName = debugName ?? "";
    }

    public abstract Assembly LoadFromPath( string assemblyPath );

    public abstract Assembly LoadFromStream( Stream peStream, Stream? pdbStream );

    // .NET 5.0 has collectible assemblies, but collectible assemblies cannot be returned to AppDomain.AssemblyResolve.
    public abstract bool IsCollectible( Assembly assembly );

    protected abstract IEnumerable<Assembly> GetAssemblies();

    public virtual void Dispose() { }

    public bool TryGetLoadedAssembly( AssemblyName assemblyName, out Assembly? existingAssembly )
    {
        // The assembly might have been already loaded. In this situation, we must use the copy that was previously loaded.

        var assembliesOfSameIdentity = this.GetAssemblies()
            .Where(
                a =>
                {
                    var candidateName = a.GetName();

                    return candidateName.Name == assemblyName.Name &&
                           (candidateName.Version ?? _defaultVersion).Equals( assemblyName.Version ?? _defaultVersion ) &&
                           (candidateName.GetPublicKeyToken() ?? []).SequenceEqual( assemblyName.GetPublicKeyToken() ?? [] );
                } )
            .ToList();

        if ( assembliesOfSameIdentity.Count >= 1 )
        {
            existingAssembly = assembliesOfSameIdentity[0];

            return true;
        }
        else
        {
            existingAssembly = null;

            return false;
        }
    }

    public override string ToString() => $"{this.GetType().Name} DebugName='{this.DebugName}'";
}