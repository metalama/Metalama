// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Aspects;

public sealed partial class InheritableAspectInstance : IAspectInstance, IAspectPredecessorImpl
{
    private readonly IAspectClass? _aspectClass;

    public IRef<IDeclaration> TargetDeclaration { get; private set; }

    IRef<IDeclaration> IAspectPredecessor.TargetDeclaration => this.TargetDeclaration;

    // This member is not available after deserialization. We would need, if necessary, to have a post-deserialization initialization.
    // The AspectClass is the full type of the aspect, anyway.
    public IAspectClass AspectClass => this._aspectClass ?? throw new InvalidOperationException();

    bool IAspectInstance.IsSkipped => false;

    public bool IsInheritable => true;

    public ImmutableArray<IAspectInstance> SecondaryInstances { get; private set; }

    ImmutableArray<AspectPredecessor> IAspectPredecessor.Predecessors => ImmutableArray<AspectPredecessor>.Empty;

    public IAspectState? AspectState { get; private set; }

    public IAspect Aspect { get; private set; }

    public int PredecessorDegree { get; }

    public InheritableAspectInstance( IAspectInstance aspectInstance )
    {
        var asPredecessor = (IAspectPredecessorImpl) aspectInstance;
        this.TargetDeclaration = asPredecessor.TargetDeclaration;
        this.TargetDeclarationDepth = asPredecessor.TargetDeclarationDepth;
        this.Aspect = aspectInstance.Aspect;
        this._aspectClass = aspectInstance.AspectClass;
        this.AspectState = aspectInstance.AspectState;
        this.PredecessorDegree = aspectInstance.PredecessorDegree + 1;

        this.SecondaryInstances = aspectInstance.SecondaryInstances.Select( i => new InheritableAspectInstance( i ) )
            .ToImmutableArray<IAspectInstance>();
    }

    private InheritableAspectInstance()
    {
        // This is the deserializing constructors. Fields are set by the deserializer, but here
        // we are suppressing warnings.
        this.TargetDeclaration = null!;
        this.SecondaryInstances = default;
        this.Aspect = null!;
        this.PredecessorDegree = 0;
    }

    public override string ToString() => $"{nameof(InheritableAspectInstance)}, Aspect={this.Aspect}, Target={this.TargetDeclaration}";

    public FormattableString FormatPredecessor( ICompilation compilation )
        => $"aspect '{this.AspectClass.ShortName}' applied to '{this.TargetDeclaration.GetTarget( compilation )}'";

    Location? IAspectPredecessorImpl.GetDiagnosticLocation( Compilation compilation ) => null;

    public int TargetDeclarationDepth { get; }

    ImmutableArray<SyntaxTree> IAspectPredecessorImpl.PredecessorTreeClosure => ImmutableArray<SyntaxTree>.Empty;
}