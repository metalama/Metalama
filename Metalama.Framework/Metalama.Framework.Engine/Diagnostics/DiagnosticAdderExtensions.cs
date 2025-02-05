// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Diagnostics
{
    public static class DiagnosticAdderExtensions
    {
        public static void Report( this IDiagnosticAdder adder, IEnumerable<Diagnostic> diagnostics )
        {
            foreach ( var d in diagnostics )
            {
                adder.Report( d );
            }
        }
    }
}