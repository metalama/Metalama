// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Diagnostics;

/// <summary>
/// Encapsulates an <see cref="IDiagnosticSink"/> and the default target of diagnostics, suppressions, and code fixes.
/// </summary>
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
    /// Reports a diagnostic to the default location of the current <see cref="ScopedDiagnosticSink"/>..
    /// </summary>
    /// <param name="diagnostic">The diagnostic to report.</param>
    public void Report( IDiagnostic diagnostic ) => this.Sink.Report( diagnostic, this.DefaultTargetLocation, this.Source );

    /// <summary>
    /// Reports a parametric diagnostic by specifying its location.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to report.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
    public void Report( IDiagnostic diagnostic, IDiagnosticLocation? location ) => this.Sink.Report( diagnostic, location, this.Source );

    /// <summary>
    /// Suppresses a diagnostic from the default declaration of the current <see cref="ScopedDiagnosticSink"/>.
    /// </summary>
    /// <param name="suppression">The suppression definition.</param>
    public void Suppress( ISuppression suppression )
    {
        this.Sink.Suppress(
            suppression,
            this.DefaultTargetDeclaration ?? throw new InvalidOperationException( "Use the overload that receives a scope declaration." ),
            this.Source );
    }

    /// <summary>
    /// Suppresses a diagnostic by specifying the declaration in which the suppression must be effective.
    /// </summary>
    /// <param name="suppression">The suppression definition, which must be defined as a static field or property.</param>
    /// <param name="scope">The declaration in which the diagnostic must be suppressed.</param>
    public void Suppress( ISuppression suppression, IDeclaration scope ) => this.Sink.Suppress( suppression, scope, this.Source );
}