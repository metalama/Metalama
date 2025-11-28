// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;
using System.Collections.Immutable;

namespace Metalama.Framework.Diagnostics;

/// <summary>
/// Represents an instance of a diagnostic, including its definition, arguments, and optional extensions.
/// </summary>
/// <remarks>
/// <para>
/// Diagnostic instances are typically created from a <see cref="DiagnosticDefinition"/> or
/// <see cref="DiagnosticDefinition{T}"/> by calling <see cref="DiagnosticDefinition{T}.WithArguments"/>.
/// The diagnostic instance is then reported using <see cref="ScopedDiagnosticSink.Report(IDiagnostic)"/>.
/// </para>
/// <para>
/// For diagnostics without parameters, use <see cref="DiagnosticDefinition"/> (without type arguments), which implements both
/// <see cref="IDiagnosticDefinition"/> and <see cref="IDiagnostic"/>, allowing it to be used directly
/// when reporting.
/// </para>
/// </remarks>
/// <seealso cref="IDiagnosticDefinition"/>
/// <seealso cref="DiagnosticDefinition"/>
/// <seealso cref="DiagnosticDefinition{T}"/>
/// <seealso cref="ScopedDiagnosticSink"/>
/// <seealso href="@diagnostics"/>
[CompileTime]
[InternalImplement]
public interface IDiagnostic
{
    /// <summary>
    /// Gets the <see cref="IDiagnosticDefinition"/> from which the current diagnostic was created.
    /// </summary>
    IDiagnosticDefinition Definition { get; }

    /// <summary>
    /// Gets the extensions (such as code fixes) associated with this diagnostic.
    /// </summary>
    ImmutableArray<IDiagnosticExtension> Extensions { get; }

    /// <summary>
    /// Gets the arguments passed to the diagnostic's message format string.
    /// </summary>
    /// <remarks>
    /// For a <see cref="DiagnosticDefinition{T}"/>, this will be an instance of type <c>T</c>.
    /// For a non-parameterized <see cref="DiagnosticDefinition"/>, this will be <c>null</c>.
    /// </remarks>
    object? Arguments { get; }

    /// <summary>
    /// Returns a copy of this diagnostic with the specified extensions.
    /// </summary>
    /// <param name="extensions">The extensions to associate with the diagnostic.</param>
    /// <returns>A new diagnostic instance with the specified extensions.</returns>
    IDiagnostic WithExtensions( ImmutableArray<IDiagnosticExtension> extensions );
}