// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime
{
    [ExcludeFromCodeCoverage] // Not used in tests.
    internal sealed class DefaultCompileTimeDomainFactory : ICompileTimeDomainFactory
    {
        private readonly GlobalServiceProvider _serviceProvider;
        private readonly object _sync = new();
        private CompileTimeDomain? _currentDomain;

        public DefaultCompileTimeDomainFactory( GlobalServiceProvider serviceProvider )
        {
            this._serviceProvider = serviceProvider;
        }

        public CompileTimeDomain CreateDomain() => new( this._serviceProvider );

        public CompileTimeDomain GetOrCreateDomain( IReadOnlyCollection<string> assemblyPaths )
        {
            lock ( this._sync )
            {
                if ( this._currentDomain != null && this._currentDomain.IsCompatibleWithAssemblies( assemblyPaths ) )
                {
                    return this._currentDomain;
                }

                // The current domain is incompatible or doesn't exist yet. Create a new one.
                // The old domain is not disposed here — it may still be in use by concurrent compilations.
                // It will be collected by the GC when no longer referenced.
                this._currentDomain = new CompileTimeDomain( this._serviceProvider );

                return this._currentDomain;
            }
        }
    }
}
