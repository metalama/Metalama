// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Engine.Diagnostics;

/// <summary>
/// A simple and early-evaluated implementation of <see cref="IDiagnosticSource"/>, for use in scenarios where performance
/// is not critical.
/// </summary>
internal sealed class AdhocDiagnosticSource : IDiagnosticSource
{
    public AdhocDiagnosticSource( string diagnosticSourceDescription )
    {
        this.DiagnosticSourceDescription = diagnosticSourceDescription;
    }

    public string DiagnosticSourceDescription { get; }
}