// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Diagnostics;

/// <summary>
/// Wraps a Roslyn <see cref="Location"/> as an <see cref="IDiagnosticLocation"/>.
/// </summary>
internal sealed class LocationWrapper : IDiagnosticLocation
{
    /// <summary>
    /// Gets the location.
    /// </summary>
    public Location? DiagnosticLocation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocationWrapper"/> class with the given <see cref="Location"/>.
    /// </summary>
    /// <param name="diagnosticLocation"></param>
    public LocationWrapper( Location? diagnosticLocation )
    {
        this.DiagnosticLocation = diagnosticLocation;
    }

    public override string ToString() => this.DiagnosticLocation?.ToString() ?? "<null>";
}