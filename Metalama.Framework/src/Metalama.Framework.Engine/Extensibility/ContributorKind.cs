// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Queries.Diagnostics;
using System;

namespace Metalama.Framework.Engine.Extensibility;

[PublicAPI]
public abstract class ContributorKind
{
    /// <summary>
    /// The backing field of <see cref="IsDesignTimeValidator"/>.
    /// </summary>
    private readonly bool _isDesignTimeValidator;

    /// <summary>
    /// The backing field of <see cref="IsProjectTransitive"/>.
    /// </summary>
    private readonly bool _isProjectTransitive = true;

    protected ContributorKind( string name )
    {
        this.Name = name;
    }

    public string Name { get; }

    internal bool IsExtension { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the design-time results of this kind are validators of declarations, indexed by the validated declaration
    /// and merged from referenced projects.
    /// </summary>
    /// <exception cref="InvalidOperationException">The kind is not project-transitive.</exception>
    public bool IsDesignTimeValidator
    {
        get => this._isDesignTimeValidator;
        init
        {
            if ( value && !this._isProjectTransitive )
            {
                throw new InvalidOperationException(
                    $"The contributor kind '{this.Name}' cannot be a design-time validator, because it is not project-transitive." );
            }

            this._isDesignTimeValidator = value;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the design-time results of this kind are exported to the projects that reference the project that produced
    /// them. The default value is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The design-time pipeline stores the design-time form of every contributor that an extension returns. When this property is <c>true</c>, it
    /// also exports this form to the projects that reference the project: it writes it to the design-time transitive manifest, and the presence of
    /// such a form makes the pipeline produce this manifest.
    /// </para>
    /// <para>
    /// When this property is <c>false</c>, the kind is project-local, and its design-time forms are excluded from both. Only
    /// <see cref="PipelineExtension.AnalyzeSemanticModel"/> of the producing project sees them, and
    /// <see cref="IDesignTimePipelineResultExtension.ToTransitiveAspectManifestExtension"/> is never called for them. The flag is read from the kind
    /// of the design-time form. The compile-time pipeline does not read it.
    /// </para>
    /// <para>
    /// A design-time validator must be project-transitive. The <c>init</c> accessors of this property and of
    /// <see cref="IsDesignTimeValidator"/> throw <see cref="InvalidOperationException"/> for a kind that is a design-time validator and not
    /// project-transitive, so that an invalid kind fails when it is declared.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">The kind is a design-time validator.</exception>
    public bool IsProjectTransitive
    {
        get => this._isProjectTransitive;
        init
        {
            if ( !value && this._isDesignTimeValidator )
            {
                throw new InvalidOperationException(
                    $"The contributor kind '{this.Name}' cannot be a design-time validator, because it is not project-transitive." );
            }

            this._isProjectTransitive = value;
        }
    }

    public abstract Type Type { get; }

    internal static ContributorKind<IAspectSource> AspectSource { get; } = new( nameof(AspectSource) ) { IsExtension = false };

    internal static ContributorKind<IHierarchicalOptionsSource> HierarchicalOptionsSource { get; } =
        new( nameof(HierarchicalOptionsSource) ) { IsExtension = false };

    internal static ContributorKind<IDiagnosticSource> DiagnosticSource { get; } = new( nameof(DiagnosticSource) );

    internal static ContributorKind<TransitiveAspectInstance> TransitiveAspectInstance { get; } =
        new( nameof(TransitiveAspectInstance) );

    internal static ContributorKind<SerializableTransitiveAspectInstance> SerializableTransitiveAspectInstance { get; } =
        new( nameof(SerializableTransitiveAspectInstance) );

    public override string ToString() => this.Name;
}

#pragma warning disable SA1402
[PublicAPI]
public sealed class ContributorKind<T> : ContributorKind
#pragma warning restore SA1402
    where T : IContributor
{
    public ContributorKind( string name ) : base( name ) { }

    public override Type Type => typeof(T);
}