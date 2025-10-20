// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Extensibility;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Aspects;

public class TransitiveAspectInstance : IAspectInstance, IAspectPredecessorImpl, ITransitivePipelineContributor, ITransitiveAspectsManifestExtension, IExtensionPipelineContributor
{
    internal TransitiveAspectInstance(
        IAspect aspect,
        IDeclaration targetDeclaration,
        IAspectClass aspectClass,
        IAspectState? aspectState,
        int predecessorDegree )
    {
        this.Aspect = aspect;
        this.TargetDeclaration = targetDeclaration.ToRef();
        this.AspectClass = aspectClass;
        this.TargetDeclarationDepth = targetDeclaration.Depth;
        this.PredecessorDegree = predecessorDegree;
        this.AspectState = aspectState;
    }

    public int PredecessorDegree { get; }

    public IRef<IDeclaration> TargetDeclaration { get; }

    public ImmutableArray<AspectPredecessor> Predecessors => [];

    public IAspect Aspect { get; }

    public IAspectClass AspectClass { get; }

    public bool IsSkipped => false;

    public bool IsInheritable => false;

    public ImmutableArray<IAspectInstance> SecondaryInstances => [];

    public IAspectState? AspectState { get; }

    public FormattableString FormatPredecessor( ICompilation compilation ) => $"{this.AspectClass.Description}";

    public Location? GetDiagnosticLocation( Compilation compilation ) => null;

    public int TargetDeclarationDepth { get; }

    public ImmutableArray<SyntaxTree> PredecessorTreeClosure => [];

    public SyntaxTree? SyntaxTree => throw new NotImplementedException();

    public IDesignTimeAspectPipelineResultExtension? ToDesignTime() => throw new NotImplementedException();

    public ContributorKind ContributorKind => ContributorKind.TransitiveAspect;
}