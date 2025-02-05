// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Diagnostics;

public interface IScopedDiagnosticSink
{
    IDiagnosticSink Sink { get; }

    IDiagnosticSource Source { get; }

    /// <summary>
    /// Reports a diagnostic to the default location of the current <see cref="ScopedDiagnosticSink"/>..
    /// </summary>
    void Report( IDiagnostic diagnostic );

    /// <summary>
    /// Suppresses a diagnostic from the default declaration of the current <see cref="ScopedDiagnosticSink"/>.
    /// </summary>
    void Suppress( ISuppression suppression );
}