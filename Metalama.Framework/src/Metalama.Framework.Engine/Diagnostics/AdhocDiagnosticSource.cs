// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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