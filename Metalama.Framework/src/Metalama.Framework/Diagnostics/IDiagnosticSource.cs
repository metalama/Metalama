// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Diagnostics;

/// <summary>
/// Represents the source of a diagnostic or suppression, typically an aspect, fabric, or other compile-time code component.
/// </summary>
/// <remarks>
/// This interface is used internally by the diagnostic system to track which aspect or compile-time component
/// reported a diagnostic or suppression. It is automatically implemented by the framework and typically not
/// used directly in user code.
/// </remarks>
/// <seealso cref="IDiagnosticSink"/>
/// <seealso cref="ScopedDiagnosticSink"/>
/// <seealso href="@diagnostics"/>
[CompileTime]
[InternalImplement]
public interface IDiagnosticSource
{
    /// <summary>
    /// Gets a description of the diagnostic source for logging and debugging purposes.
    /// </summary>
    string DiagnosticSourceDescription { get; }
}