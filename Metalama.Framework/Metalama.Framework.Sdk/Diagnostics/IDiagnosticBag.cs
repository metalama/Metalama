// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Diagnostics;

/// <summary>
/// Allows both to report and to read diagnostics.
/// </summary>
public interface IDiagnosticBag : IDiagnosticAdder, IReadOnlyCollection<Diagnostic>
{
    /// <summary>
    /// Clears the collection.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets a value indicating whether the collection contains at least one error.
    /// </summary>
    bool HasError { get; }
}