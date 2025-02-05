// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Diagnostics;

internal sealed class ThrowingDiagnosticAdder : IDiagnosticAdder
{
    public void Report( Diagnostic diagnostic )
    {
        if ( diagnostic.Severity == DiagnosticSeverity.Error )
        {
            throw new DiagnosticException( diagnostic );
        }
    }

    public static IDiagnosticAdder Instance { get; } = new ThrowingDiagnosticAdder();
}