// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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