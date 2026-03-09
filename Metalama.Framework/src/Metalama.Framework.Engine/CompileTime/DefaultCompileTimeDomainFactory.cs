// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime
{
    [ExcludeFromCodeCoverage] // Not used in tests.
    internal sealed class DefaultCompileTimeDomainFactory : ICompileTimeDomainFactory
    {
        private readonly GlobalServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<Guid, WeakReference<CompileTimeDomain>> _domains = new();

        public DefaultCompileTimeDomainFactory( GlobalServiceProvider serviceProvider )
        {
            this._serviceProvider = serviceProvider;
        }

        public CompileTimeDomain CreateDomain()
        {
            var domain = new CompileTimeDomain( this._serviceProvider );
            this._domains.TryAdd( domain.Guid, new WeakReference<CompileTimeDomain>( domain ) );

            return domain;
        }

        public CompileTimeDomain GetOrCreateDomain( IReadOnlyCollection<string> assemblyPaths )
        {
            // Clean up dead references and check for a compatible domain among all still-alive domains.
            foreach ( var kvp in this._domains.ToArray() )
            {
                if ( !kvp.Value.TryGetTarget( out var domain ) )
                {
                    // Domain has been collected by GC. Remove the dead reference.
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
