// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Metalama.Testing.UnitTesting;

internal sealed class TestCompileTimeDomainFactory : ICompileTimeDomainFactory
{
    private readonly GlobalServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Guid, WeakReference<CompileTimeDomain>> _domains = new();
    private readonly object _lock = new();

    public TestCompileTimeDomainFactory( GlobalServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
    }

    public CompileTimeDomain CreateDomain()
    {
        CompileTimeDomain domain;

#if NET5_0_OR_GREATER
        var unloadableDomain = new UnloadableCompileTimeDomain( this._serviceProvider );
        unloadableDomain.UnloadError += _ => MemoryDumpHelper.CaptureMiniDumpOnce();
        domain = unloadableDomain;
#else
        domain = new CompileTimeDomain( this._serviceProvider );
#endif

        this._domains.TryAdd( domain.Guid, new WeakReference<CompileTimeDomain>( domain ) );

        return domain;
    }

    public CompileTimeDomain GetOrCreateDomain( IReadOnlyCollection<string> assemblyPaths )
    {
        lock ( this._lock )
        {
            // Clean up dead references and check for a compatible domain among all still-alive domains.
            foreach ( var kvp in this._domains.ToArray() )
            {
                if ( !kvp.Value.TryGetTarget( out var domain ) )
                {
                    this._domains.TryRemove( kvp.Key, out _ );

                    continue;
                }

                if ( domain.IsCompatibleWithAssemblies( assemblyPaths ) )
                {
                    return domain;
                }
            }

            // No compatible domain found. Create a new one.
            return this.CreateDomain();
        }
    }
}
