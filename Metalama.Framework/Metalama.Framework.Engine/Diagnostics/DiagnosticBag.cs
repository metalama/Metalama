// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Engine.Diagnostics
{
    public sealed class DiagnosticBag : IDiagnosticBag
    {
        private volatile ConcurrentQueue<Diagnostic>? _bag;

        public bool HasError { get; private set; }

        private ConcurrentQueue<Diagnostic> GetBag()
        {
            if ( this._bag != null )
            {
                return this._bag;
            }
            else
            {
                Interlocked.CompareExchange( ref this._bag, new ConcurrentQueue<Diagnostic>(), null );

                return this._bag;
            }
        }

        public void Report( Diagnostic diagnostic )
        {
            this.GetBag().Enqueue( diagnostic );

            if ( diagnostic.Severity == DiagnosticSeverity.Error )
            {
                this.HasError = true;
            }
        }

        public IEnumerator<Diagnostic> GetEnumerator() => this._bag?.GetEnumerator() ?? Enumerable.Empty<Diagnostic>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int Count => this._bag?.Count ?? 0;

        public void Clear() => this._bag = null;

        public override string ToString() => $"DiagnosticBag Count={this.Count}";
    }
}