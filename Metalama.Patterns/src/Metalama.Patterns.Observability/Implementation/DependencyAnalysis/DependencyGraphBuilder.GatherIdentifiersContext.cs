// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// To avoid confusion due to some identical type names, this file is concerned only with Roslyn types.
// Do not add usings for the namespaces Metalama.Framework.Code, Metalama.Framework.Engine.CodeModel or
// related namespaces.

using Metalama.Framework.Aspects;
using Microsoft.CodeAnalysis;

namespace Metalama.Patterns.Observability.Implementation.DependencyAnalysis;

internal partial class DependencyGraphBuilder
{
    [CompileTime]
    private abstract class GatherIdentifiersContext : IDisposable
    {
        private List<DependencyPathElement>? _symbols;
        private List<ForkItem>? _forks;

        internal sealed class ForkItem
        {
            public GatherIdentifiersContext Fork { get; set; } = null!;

            public bool IsJoined { get; set; }
        }

        protected GatherIdentifiersContext( IReadOnlyCollection<DependencyPathElement>? initialSymbols = null )
        {
            this._symbols = initialSymbols == null ? null : new List<DependencyPathElement>( initialSymbols );
        }

        public abstract void Dispose();

        public int StartDepth { get; private set; }

        public IReadOnlyList<DependencyPathElement>? Symbols => this._symbols;

        public virtual IEnumerable<IReadOnlyList<DependencyPathElement>> SymbolsForAllForks()
            => throw new NotSupportedException( nameof(this.SymbolsForAllForks) + " must only be called on a root " + nameof(GatherIdentifiersContext) + "." );

        public abstract bool IsRoot { get; }

        protected virtual void ThrowIfJoined() { }

        protected bool HasUnjoinedForks => this._forks != null && this._forks.Any( f => !f.IsJoined );

        protected abstract RootGatherIdentifiersContext RootContext { get; }

        /// <summary>
        /// Creates and prepares a fork of the current <see cref="GatherIdentifiersContext"/>.
        /// </summary>
        /// <param name="startDepth">The current depth in the syntax tree being processed.</param>
        public PreparedFork PrepareFork( int startDepth )
        {
            this.ThrowIfJoined();

            if ( startDepth < this.StartDepth )
            {
                throw new ArgumentException(
                    "Must be greater than or equal to the " + nameof(this.StartDepth) + " of the current " + nameof(GatherIdentifiersContext) + ".",
                    nameof(startDepth) );
            }

            var forkItem = new ForkItem();
            var fork = new ForkedGatherIdentifiersContext( this.RootContext, this._symbols, forkItem ) { StartDepth = startDepth };
            forkItem.Fork = fork;

            this.RootContext.AddToAllForks( forkItem );
            (this._forks ??= new List<ForkItem>()).Add( forkItem );

            return new PreparedFork( fork, this.RootContext.Manager );
        }

        public void EnsureStarted( int startDepth )
        {
            this.ThrowIfJoined();

            if ( startDepth <= 0 )
            {
                throw new ArgumentException( "Must be greater than zero." );
            }

            if ( this.StartDepth == 0 )
            {
                this.StartDepth = startDepth;
            }
            else
            {
                if ( startDepth < this.StartDepth )
                {
                    // Indicates a problem with dependency analysis program design.
                    throw new InvalidOperationException( nameof(this.EnsureStarted) + " called with a depth less than the actual StartDepth." );
                }
            }
        }

        public void Reset()
        {
            this.ThrowIfJoined();

            if ( this.HasUnjoinedForks )
            {
                throw new InvalidOperationException( "The current " + nameof(GatherIdentifiersContext) + " has unjoined forks so cannot be reset." );
            }

            this.StartDepth = 0;
            this._symbols?.Clear();
            this._forks?.Clear();
        }

        public void AddSymbol( ISymbol symbol, SyntaxNode node )
        {
            this.ThrowIfJoined();
            var record = new DependencyPathElement( symbol, node );
            this.AddSymbolUnsafe( record );
        }

        private void AddSymbolUnsafe( in DependencyPathElement reference )
        {
            (this._symbols ??= new List<DependencyPathElement>()).Add( reference );

            if ( this._forks != null )
            {
                foreach ( var f in this._forks )
                {
                    if ( f.IsJoined )
                    {
                        f.Fork.AddSymbolUnsafe( reference );
                    }
                }
            }
        }
    }
}