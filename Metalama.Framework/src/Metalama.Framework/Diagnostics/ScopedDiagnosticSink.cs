// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Diagnostics;

/// <summary>
/// A diagnostic sink that combines an <see cref="IDiagnosticSink"/> with default target location and declaration,
/// simplifying diagnostic reporting and suppression.
/// </summary>
/// <remarks>
/// <para>
/// This struct maintains a default target location and declaration, allowing diagnostics and suppressions
/// to be reported without explicitly specifying where they apply. This is the primary way aspect code
/// interacts with the diagnostic system, accessed via <see cref="IAdviser.Diagnostics"/>.
/// </para>
/// <para>
/// The <see cref="Report(IDiagnostic)"/> method reports a diagnostic to the default location, while the
/// <see cref="Suppress(ISuppression)"/> method suppresses diagnostics within the default declaration scope.
/// Overloads allow specifying different locations or scopes when needed.
/// </para>
/// </remarks>
/// <seealso cref="IDiagnosticSink"/>
/// <seealso cref="IScopedDiagnosticSink"/>
/// <seealso cref="IDiagnostic"/>
/// <seealso cref="IAdviser"/>
/// <seealso href="@diagnostics"/>
[PublicAPI]
[CompileTime]
public readonly struct ScopedDiagnosticSink : IScopedDiagnosticSink
{
    /// <summary>
    /// Gets the underlying diagnostic sink.
    /// </summary>
    public IDiagnosticSink Sink { get; }

    /// <summary>
    /// Gets the source of diagnostics, suppressions, or code fixes.
    /// </summary>
    public IDiagnosticSource Source { get; }

    /// <summary>
    /// Gets the declaration on which diagnostics or code fixes will be reported or suppressed.
    /// </summary>
    public IDeclaration? DefaultTargetDeclaration { get; }

    /// <summary>
    /// Gets the location on which diagnostics or code fixes will be reported or suppressed.
    /// </summary>
    public IDiagnosticLocation? DefaultTargetLocation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedDiagnosticSink"/> struct.
    /// </summary>
    /// <param name="sink">The underlying diagnostic sink.</param>
    /// <param name="source">The source of diagnostics, suppressions, or code fixes.</param>
    /// <param name="defaultTargetLocation">The default location on which diagnostics or code fixes will be reported or suppressed.</param>
    /// <param name="defaultTargetDeclaration">The default declaration on which diagnostics or code fixes will be reported or suppressed.</param>
    public ScopedDiagnosticSink(
        IDiagnosticSink sink,
        IDiagnosticSource source,
        IDiagnosticLocation? defaultTargetLocation,
        IDeclaration? defaultTargetDeclaration )
    {
        this.Sink = sink;
        this.Source = source;
        this.DefaultTargetLocation = defaultTargetLocation;
        this.DefaultTargetDeclaration = defaultTargetDeclaration;
    }

    /// <summary>
    /// Reports a diagnostic to the default location of this scoped sink.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to report, created from a <see cref="DiagnosticDefinition"/> or <see cref="DiagnosticDefinition{T}"/>.</param>
    public void Report( IDiagnostic diagnostic ) => this.Sink.Report( diagnostic, this.DefaultTargetLocation, this.Source );

    /// <summary>
    /// Reports a diagnostic to a specific location instead of the default location.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to report.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
    public void Report( IDiagnostic diagnostic, IDiagnosticLocation? location ) => this.Sink.Report( diagnostic, location, this.Source );

    /// <summary>
    /// Suppresses a diagnostic within the default declaration scope of this scoped sink.
    /// </summary>
    /// <param name="suppression">The suppression definition, which must be defined as a static field in a compile-time class.</param>
    public void Suppress( ISuppression suppression )
    {
        this.Sink.Suppress(
            suppression,
            this.DefaultTargetDeclaration ?? throw new InvalidOperationException( "Use the overload that receives a scope declaration." ),
            this.Source );
    }

    /// <summary>
    /// Suppresses a diagnostic within a specific declaration scope instead of the default scope.
    /// </summary>
    /// <param name="suppression">The suppression definition, which must be defined as a static field in a compile-time class.</param>
    /// <param name="scope">The declaration within which the diagnostic should be suppressed.</param>
    public void Suppress( ISuppression suppression, IDeclaration scope ) => this.Sink.Suppress( suppression, scope, this.Source );
}