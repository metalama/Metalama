// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CompileTime
{
    /// <summary>
    /// Shared implementation of <see cref="ICompileTimeDomainFactory"/>: keeps the live domains and decides which one a
    /// set of assemblies can be loaded into. Only the creation of the domain itself differs between implementations.
    /// </summary>
    /// <remarks>
    /// The selection logic lives here rather than in each implementation because it used to be duplicated between the
    /// production factory and the test one, and the two drifted: a fix applied to production was invisible to every unit
    /// test, so the tests kept passing against the old behaviour. See #1749.
    /// </remarks>
    public abstract class CompileTimeDomainFactoryBase : ICompileTimeDomainFactory
    {
        private readonly ConcurrentDictionary<Guid, WeakReference<CompileTimeDomain>> _domains = new();
        private readonly object _lock = new();

        /// <summary>
        /// Creates a domain, without registering it.
        /// </summary>
        protected abstract CompileTimeDomain CreateDomainCore();

        public CompileTimeDomain CreateDomain()
        {
            var domain = this.CreateDomainCore();

            // ReSharper disable once InconsistentlySynchronizedField
            this._domains.TryAdd( domain.Guid, new WeakReference<CompileTimeDomain>( domain ) );

            return domain;
        }

        /// <summary>
        /// Returns a domain into which the given assemblies can be loaded, reusing one when possible.
        /// </summary>
        /// <remarks>
        /// The chosen domain <em>reserves</em> the identities before this returns, and it does so under the lock. A
        /// caller loads its assemblies long after it has been given a domain, so asking only whether the
        /// already-loaded assemblies conflict would hand one domain to two projects that both intend to load
        /// conflicting versions, and the second would fail with "Assembly with same name is already loaded". That is a
        /// check-then-act gap rather than a data race: it happens even when the two requests are strictly sequential.
        /// See #1749.
        /// </remarks>
        public CompileTimeDomain GetOrCreateDomain( IReadOnlyCollection<string> assemblyPaths )
        {
            lock ( this._lock )
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

                    if ( domain.TryReserveAssemblies( assemblyPaths ) )
                    {
                        return domain;
                    }
                }

                // No compatible domain found. Create a new one.
                var newDomain = this.CreateDomain();

                // Cannot fail on a domain that has just been created, but the reservation is what makes the next caller
                // see these identities, so it must happen here too.
                newDomain.TryReserveAssemblies( assemblyPaths );

                return newDomain;
            }
        }
    }
}
