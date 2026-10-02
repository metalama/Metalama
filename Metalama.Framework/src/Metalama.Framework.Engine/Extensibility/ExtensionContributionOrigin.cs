// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Queries;
using System;

namespace Metalama.Framework.Engine.Extensibility;

/// <summary>
/// Represents the aspect or fabric that made a contribution to an extension, together with the default template provider and the information
/// that the engine needs to attribute and order the code that the contribution produces.
/// </summary>
/// <remarks>
/// <para>
/// An origin captured from an aspect or from a type fabric references the aspect instance of the current pipeline execution. A contributor
/// that holds it must not outlive the execution, and the design-time form of a contributor must not hold it.
/// </para>
/// <para>
/// An origin captured from a project fabric or a namespace fabric can be stored in the pipeline configuration, which is long-lived at design
/// time. Such an origin therefore holds only the predecessor, the description, the default template provider and the identifier of the aspect
/// layer. It holds no aspect instance and no template class instance.
/// </para>
/// </remarks>
[PublicAPI]
public sealed class ExtensionContributionOrigin
{
    internal ExtensionContributionOrigin(
        AspectPredecessor predecessor,
        string diagnosticSourceDescription,
        TemplateProvider? defaultTemplateProvider,
        TemplateClassInstance? templateClassInstance,
        AspectLayerId aspectLayerId,
        IAspectInstanceInternal? aspectInstance )
    {
        this.Predecessor = predecessor;
        this.DiagnosticSourceDescription = diagnosticSourceDescription;
        this.DefaultTemplateProvider = defaultTemplateProvider;
        this.TemplateClassInstance = templateClassInstance;
        this.AspectLayerId = aspectLayerId;
        this.AspectInstance = aspectInstance;
    }

    /// <summary>
    /// Gets the aspect instance or the fabric instance that made the contribution.
    /// </summary>
    public AspectPredecessor Predecessor { get; }

    /// <summary>
    /// Gets a human-readable description of the aspect or fabric, used in diagnostics.
    /// </summary>
    public string DiagnosticSourceDescription { get; }

    /// <summary>
    /// Gets the default template provider of the contribution, or <c>null</c>.
    /// </summary>
    public TemplateProvider? DefaultTemplateProvider { get; }

    /// <summary>
    /// Captures the origin of a contribution made through a query, for example through a fabric amender or <see cref="IAspectBuilder{TAspectTarget}.Outbound"/>.
    /// </summary>
    /// <param name="owner">The owner of the query.</param>
    /// <exception cref="ArgumentException">The owner was not created by the Metalama engine.</exception>
    public static ExtensionContributionOrigin Capture( IQueryOwner owner )
        => owner is IExtensionContributionOriginSource source
            ? source.CaptureContributionOrigin()
            : throw new ArgumentException( "The owner was not created by the Metalama engine.", nameof(owner) );

    /// <summary>
    /// Gets the template class instance of the contribution, or <c>null</c> for an origin captured from a project or namespace fabric.
    /// </summary>
    internal TemplateClassInstance? TemplateClassInstance { get; }

    /// <summary>
    /// Gets the aspect layer to which the code produced by the contribution is attributed. It is always one of the ordered layers of the pipeline.
    /// </summary>
    internal AspectLayerId AspectLayerId { get; }

    /// <summary>
    /// Gets the aspect instance of the contribution, or <c>null</c> for an origin captured from a project or namespace fabric.
    /// </summary>
    internal IAspectInstanceInternal? AspectInstance { get; }

    public override string ToString() => this.DiagnosticSourceDescription;
}
