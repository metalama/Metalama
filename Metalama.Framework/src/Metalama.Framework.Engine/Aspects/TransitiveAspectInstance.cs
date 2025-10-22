// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Extensibility;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Aspects;

internal class TransitiveAspectInstance : ITransitivePipelineContributor, IExtensionPipelineContributor, IDesignTimePipelineResultExtension
{
    internal TransitiveAspectInstance(
        IAspect aspect,
        IRef<IDeclaration> targetDeclaration,
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

    public IRef<IDeclaration> TargetDeclaration { get; }

    public IAspect Aspect { get; }

    public IAspectClassImpl AspectClass { get; }

    public IAspectState? AspectState { get; set; }

    public int TargetDeclarationDepth { get; }

    public SyntaxTree? SyntaxTree { get; }

    public IDesignTimePipelineResultExtension? ToDesignTime() => this;

    public ContributorKind ContributorKind => ContributorKind.TransitiveAspectInstance;

    ITransitiveAspectsManifestExtension IDesignTimePipelineResultExtension.ToTransitiveAspectManifestExtension()
        => new SerializableTransitiveAspectInstance( this );
}