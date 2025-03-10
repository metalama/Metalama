// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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