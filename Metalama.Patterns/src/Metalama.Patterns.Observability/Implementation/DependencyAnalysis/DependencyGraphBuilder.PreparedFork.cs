// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// To avoid confusion due to some identical type names, this file is concerned only with Roslyn types.
// Do not add usings for the namespaces Metalama.Framework.Code, Metalama.Framework.Engine.CodeModel or
// related namespaces.

using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Observability.Implementation.DependencyAnalysis;

internal partial class DependencyGraphBuilder
{
    [CompileTime]
    private readonly struct PreparedFork
    {
        private readonly GatherIdentifiersContext? _fork;
        private readonly IGatherIdentifiersContextManagerImpl? _manager;

        public PreparedFork( GatherIdentifiersContext fork, IGatherIdentifiersContextManagerImpl manager )
        {
            this._fork = fork;
            this._manager = manager;
        }

        public GatherIdentifiersContext Use()
        {
            if ( this._manager == null )
            {
                throw new InvalidOperationException( "The object is not initialized." );
            }

            this._manager.Push( this._fork! );

            return this._fork!;
        }
    }
}