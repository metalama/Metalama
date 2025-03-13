// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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