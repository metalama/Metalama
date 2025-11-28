// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Diagnostics;

/// <summary>
/// Defines a diagnostic suppression that can be applied by aspect code to prevent specific compiler or analyzer diagnostics
/// from being reported in target code.
/// </summary>
/// <remarks>
/// <para>
/// Suppression definitions must be declared as static fields in compile-time classes (such as aspect classes,
/// fabric classes, or other compile-time helper classes), similar to <see cref="DiagnosticDefinition"/>.
/// They specify the diagnostic ID to suppress (e.g., "CS0169") and optionally a justification for the suppression.
/// </para>
/// <para>
/// Suppressions are applied using <see cref="ScopedDiagnosticSink.Suppress(ISuppression)"/> or
/// <see cref="IDiagnosticSink.Suppress"/> within a declaration scope.
/// This prevents the C# compiler or analyzers from reporting diagnostics that become irrelevant due to aspect transformations.
/// </para>
/// <para>
/// Use <see cref="WithFilter"/> to create a filtered suppression that only suppresses diagnostics matching specific criteria,
/// such as diagnostics with particular message text or arguments.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// private static readonly SuppressionDefinition _suppressUnusedField = new(
///     "CS0169",
///     "Field is used in generated code");
///
/// // In BuildAspect:
/// builder.Diagnostics.Suppress(_suppressUnusedField);
/// </code>
/// </example>
/// <seealso cref="ISuppression"/>
/// <seealso cref="IDiagnosticSink"/>
/// <seealso cref="ScopedDiagnosticSink"/>
/// <seealso href="@diagnostics"/>
[CompileTime]
[PublicAPI]
public sealed class SuppressionDefinition : ISuppression
{
    /// <summary>
    /// Gets the ID of the diagnostic to be suppressed (e.g. <c>CS0169</c>).
    /// </summary>
    public string SuppressedDiagnosticId { get; }

    /// <summary>
    /// Gets the justification for the suppression.
    /// </summary>
    public string? Justification { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SuppressionDefinition"/> class.
    /// </summary>
    /// <param name="suppressedDiagnosticId">The ID of the diagnostic to be suppressed (e.g. <c>CS0169</c>).</param>
    /// <param name="justification">Optional justification for the suppression.</param>
    public SuppressionDefinition( string suppressedDiagnosticId, string? justification = null )
    {
        this.SuppressedDiagnosticId = suppressedDiagnosticId;
        this.Justification = justification;
    }

    SuppressionDefinition ISuppression.Definition => this;

    Func<ISuppressibleDiagnostic, bool>? ISuppression.Filter => null;

    /// <summary>
    /// Creates a filtered suppression that only suppresses diagnostics matching the specified predicate.
    /// </summary>
    /// <param name="filter">A predicate that determines which diagnostics should be suppressed. The predicate receives
    /// an <see cref="ISuppressibleDiagnostic"/> and returns <c>true</c> to suppress the diagnostic or <c>false</c> to allow it.</param>
    /// <returns>A new suppression instance that applies only to diagnostics matching the filter criteria.</returns>
    /// <remarks>
    /// Use this method to selectively suppress diagnostics based on their message text, arguments, or other properties,
    /// rather than suppressing all diagnostics with the same ID.
    /// </remarks>
    public ISuppression WithFilter( Func<ISuppressibleDiagnostic, bool> filter ) => new SuppressionImpl( this, filter );

    public override string ToString() => $"suppress {this.SuppressedDiagnosticId}";
}