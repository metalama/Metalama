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
    private interface IGatherIdentifiersContextManagerClient
    {
        void OnRootContextPopped( GatherIdentifiersContext context );
    }

    private interface IGatherIdentifiersContextManagerImpl
    {
        void Push( GatherIdentifiersContext context );

        void Pop( GatherIdentifiersContext expected );
    }

    [CompileTime]
    private sealed class GatherIdentifiersContextManager : IGatherIdentifiersContextManagerImpl
    {
        private readonly IGatherIdentifiersContextManagerClient _client;
        private readonly Stack<GatherIdentifiersContext> _contexts;

        public GatherIdentifiersContextManager( IGatherIdentifiersContextManagerClient client )
        {
            this._client = client;
            this._contexts = new Stack<GatherIdentifiersContext>();
            this._contexts.Push( new RootGatherIdentifiersContext( this ) );
        }

        public GatherIdentifiersContext Current => this._contexts.Peek();

        public GatherIdentifiersContext UseNewRootContext()
        {
            var ctx = new RootGatherIdentifiersContext( this );
            this._contexts.Push( ctx );

            return ctx;
        }

        void IGatherIdentifiersContextManagerImpl.Push( GatherIdentifiersContext context )
        {
            this._contexts.Push( context );
        }

        void IGatherIdentifiersContextManagerImpl.Pop( GatherIdentifiersContext expected )
        {
            if ( this._contexts.Peek() != expected )
            {
                throw new InvalidOperationException( "The expected context is not at the top of the stack." );
            }

            this._contexts.Pop();

            if ( expected.IsRoot )
            {
                this._client.OnRootContextPopped( expected );
            }
        }
    }
}