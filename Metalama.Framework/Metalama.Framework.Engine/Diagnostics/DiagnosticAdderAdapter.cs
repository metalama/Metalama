// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System;
using System.Threading;

namespace Metalama.Framework.Engine.Diagnostics
{
    internal sealed class DiagnosticAdderAdapter : IDiagnosticAdder
    {
        private readonly Action<Diagnostic> _action;
        private int _errors; // For debugging only.

        public DiagnosticAdderAdapter( Action<Diagnostic> action )
        {
            this._action = action;
        }

        public void Report( Diagnostic diagnostic )
        {
            if ( diagnostic.Severity == DiagnosticSeverity.Error )
            {
                Interlocked.Increment( ref this._errors );
            }

            this._action( diagnostic );
        }

        public override string ToString() => $"{this._errors} error(s)";
    }
}