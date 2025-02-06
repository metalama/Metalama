// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// To avoid confusion due to some identical type names, this file is concerned only with Roslyn types.
// Do not add usings for the namespaces Metalama.Framework.Code, Metalama.Framework.Engine.CodeModel or
// related namespaces.

using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Observability.Implementation.DependencyAnalysis;

internal partial class DependencyGraphBuilder
{
    [CompileTime]
    private sealed class RootGatherIdentifiersContext : GatherIdentifiersContext
    {
        private List<ForkItem>? _allForks;

        public RootGatherIdentifiersContext( IGatherIdentifiersContextManagerImpl manager )
        {
            this.Manager = manager;
        }

        public override void Dispose()
        {
            this.Manager.Pop( this );
        }

        protected override RootGatherIdentifiersContext RootContext => this;

        public IGatherIdentifiersContextManagerImpl Manager { get; }

        public override bool IsRoot => true;

        public override IEnumerable<IReadOnlyList<DependencyPathElement>> SymbolsForAllForks()
        {
            if ( this._allForks != null && this._allForks.Any( f => !f.IsJoined ) )
            {
                throw new InvalidOperationException( "The root " + nameof(GatherIdentifiersContext) + " has unjoined forks." );
            }

            if ( this.Symbols is { Count: > 0 } )
            {
                yield return this.Symbols;
            }

            if ( this._allForks != null )
            {
                foreach ( var f in this._allForks )
                {
                    if ( f.Fork.Symbols is { Count: > 0 } )
                    {
                        yield return f.Fork.Symbols;
                    }
                }
            }
        }

        internal void AddToAllForks( ForkItem forkItem )
        {
            (this.RootContext._allForks ??= new List<ForkItem>()).Add( forkItem );
        }
    }
}