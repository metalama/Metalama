// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Extensibility;

namespace Metalama.Framework.Engine.Aspects;

/// <remarks>
/// A contributor, produced by one run of the pipeline and discarded with it, so it is under no durability constraint.
/// Its design-time form is <see cref="DesignTimeTransitiveAspectInstance"/>, which is a separate object because that
/// one is stored per file and carried forward across edits.
/// </remarks>
internal sealed class TransitiveAspectInstance : ITransitivePipelineContributor, IExtensionPipelineContributor
{
    internal TransitiveAspectInstance(
        IAspect aspect,
        IDurableRef<IDeclaration> targetDeclaration,
        int targetDeclarationDepth,
        IAspectClassImpl aspectClass,
        IAspectState? aspectState,
        int predecessorDegree,
        DocumentKey documentKey )
    {
        this.Aspect = aspect;
        this.TargetDeclaration = targetDeclaration;
        this.AspectClass = aspectClass;
        this.TargetDeclarationDepth = targetDeclarationDepth;
        this.PredecessorDegree = predecessorDegree;
        this.AspectState = aspectState;
        this.DocumentKey = documentKey;
    }

    public int PredecessorDegree { get; }

    /// <summary>
    /// Gets the declaration this aspect instance applies to.
    /// </summary>
    /// <remarks>
    /// Durable although this class no longer has to be, because the value is copied into
    /// <see cref="DesignTimeTransitiveAspectInstance"/>, which does. Converting where the reference is created rather
    /// than at the boundary keeps the requirement next to the call that has the declaration. See issue #1797.
    /// </remarks>
    public IDurableRef<IDeclaration> TargetDeclaration { get; }

    public IAspect Aspect { get; }

    public IAspectClassImpl AspectClass { get; }

    public IAspectState? AspectState { get; }

    public int TargetDeclarationDepth { get; }

    public DocumentKey DocumentKey { get; }

    public IDesignTimePipelineResultExtension ToDesignTime() => new DesignTimeTransitiveAspectInstance( this );

    public ContributorKind ContributorKind => ContributorKind.TransitiveAspectInstance;
}
