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
    private sealed class ForkedGatherIdentifiersContext : GatherIdentifiersContext, IDisposable
    {
        private readonly ForkItem _forkItem;

        public ForkedGatherIdentifiersContext(
            RootGatherIdentifiersContext rootContext,
            IReadOnlyCollection<DependencyPathElement>? parentSymbols,
            ForkItem forkItem )
            : base( parentSymbols )
        {
            this.RootContext = rootContext;
            this._forkItem = forkItem;
        }

        public override bool IsRoot => false;

        private void PopAndJoin()
        {
            this.ThrowIfJoined();

            if ( this.HasUnjoinedForks )
            {
                throw new InvalidOperationException( "The current " + nameof(GatherIdentifiersContext) + " has unjoined forks, so cannot be joined." );
            }

            this.RootContext.Manager.Pop( this );
            this._forkItem.IsJoined = true;
        }

        protected override RootGatherIdentifiersContext RootContext { get; }

        protected override void ThrowIfJoined()
        {
            if ( this._forkItem.IsJoined )
            {
                throw new NotSupportedException(
                    "The " + nameof(GatherIdentifiersContext) + " fork has already been rejoined to its parent and cannot be modified directly." );
            }
        }

        public override void Dispose()
        {
            this.PopAndJoin();
        }
    }
}