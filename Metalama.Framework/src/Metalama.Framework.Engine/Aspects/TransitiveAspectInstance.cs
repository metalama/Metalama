// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Extensibility;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Aspects;

internal sealed class TransitiveAspectInstance : ITransitivePipelineContributor, IExtensionPipelineContributor, IDesignTimePipelineResultExtension
{
    internal TransitiveAspectInstance(
        IAspect aspect,
        IDurableRef<IDeclaration> targetDeclaration,
        int targetDeclarationDepth,
        IAspectClassImpl aspectClass,
        IAspectState? aspectState,
        int predecessorDegree,
        SyntaxTree? syntaxTree )
    {
        this.Aspect = aspect;
        this.TargetDeclaration = targetDeclaration;
        this.AspectClass = aspectClass;
        this.TargetDeclarationDepth = targetDeclarationDepth;
        this.PredecessorDegree = predecessorDegree;
        this.AspectState = aspectState;
        this.SyntaxTree = syntaxTree;
    }

    public int PredecessorDegree { get; }

    /// <summary>
    /// Gets the declaration this aspect instance applies to.
    /// </summary>
    /// <remarks>
    /// Typed as <see cref="IDurableRef{T}"/> rather than <see cref="IRef{T}"/> on purpose. An instance of this class is
    /// stored in the design-time result of the file declaring its target, and the pipeline carries that result forward
    /// to every later version of the project, so a compilation-bound reference here would pin the version it was
    /// produced in for the whole editing session. Declaring the requirement in the type makes it the compiler's
    /// business rather than the caller's discipline. See issue #1797.
    /// </remarks>
    public IDurableRef<IDeclaration> TargetDeclaration { get; }

    public IAspect Aspect { get; }

    public IAspectClassImpl AspectClass { get; }

    public IAspectState? AspectState { get; }

    public int TargetDeclarationDepth { get; }

    /// <remarks>
    /// This member reports LAMA0870, deliberately left unsuppressed as a problem to be solved. It belongs to
    /// <see cref="ITransitivePipelineContributor"/>, which is under no durability constraint, but
    /// <see cref="ToDesignTime"/> returns <c>this</c>, so the tree survives into the object that the design-time
    /// pipeline stores per file and carries forward. <c>SplitResultsByTree</c> converts it to a
    /// <c>DocumentKey</c> before calling <see cref="ToDesignTime"/>, so the design-time form never needs it. The
    /// repair has to keep the transitive manifest working and flips a control in
    /// <c>TransitiveContributorMemoryLeakTests</c>. See #1830.
    /// </remarks>
    public SyntaxTree? SyntaxTree { get; }

    public IDesignTimePipelineResultExtension ToDesignTime() => this;

    public ContributorKind ContributorKind => ContributorKind.TransitiveAspectInstance;

    ITransitiveAspectsManifestExtension IDesignTimePipelineResultExtension.ToTransitiveAspectManifestExtension()
        => new SerializableTransitiveAspectInstance( this );
}