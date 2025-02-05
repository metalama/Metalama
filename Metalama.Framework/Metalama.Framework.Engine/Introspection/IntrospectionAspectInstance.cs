// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Introspection;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Introspection;

internal sealed class IntrospectionAspectInstance : IIntrospectionAspectInstance, IIntrospectionAspectPredecessorInternal
{
    private readonly ConcurrentLinkedList<AspectPredecessor> _successors = new();
    private readonly IAspectInstance _aspectInstance;

    public AspectInstanceResult? AspectInstanceResult { get; internal set; }

    public void AddSuccessor( AspectPredecessor aspectInstance ) => this._successors.Add( aspectInstance );

    private CompilationModel Compilation { get; }

    private IntrospectionFactory Factory { get; }

    public IntrospectionAspectInstance( IAspectInstance aspectInstance, CompilationModel compilation, IntrospectionFactory factory )
    {
        this._aspectInstance = aspectInstance;
        this.Compilation = compilation;
        this.Factory = factory;
    }

    public IAspect Aspect => this._aspectInstance.Aspect;

    [Memo]
    public IIntrospectionAspectClass AspectClass => this.Factory.GetIntrospectionAspectClass( this._aspectInstance.AspectClass );

    public List<IntrospectionAdvice> Advice { get; } = new();

    IReadOnlyList<IIntrospectionAdvice> IIntrospectionAspectInstance.Advice => this.Advice;

    public bool IsSkipped => this._aspectInstance.IsSkipped;

    [Memo]
    public ImmutableArray<IIntrospectionAspectInstance> SecondaryInstances
        => this._aspectInstance.SecondaryInstances.Select( x => this.Factory.GetIntrospectionAspectInstance( x ) )
            .ToImmutableArray<IIntrospectionAspectInstance>();

    public IAspectState? AspectState => this._aspectInstance.AspectState;

    [Memo]
    public ImmutableArray<IIntrospectionDiagnostic> Diagnostics => this.GetDiagnostics();

    private ImmutableArray<IIntrospectionDiagnostic> GetDiagnostics()
    {
        var result = this.AspectInstanceResult;

        if ( result == null )
        {
            return ImmutableArray<IIntrospectionDiagnostic>.Empty;
        }

        return result.Diagnostics.ReportedDiagnostics.ToIntrospectionDiagnostics(
            this.Compilation,
            IntrospectionDiagnosticSource.Metalama );
    }

    public int PredecessorDegree => this._aspectInstance.PredecessorDegree;

    [Memo]
    public IDeclaration TargetDeclaration => this._aspectInstance.TargetDeclaration.GetTarget( this.Compilation );

    [Memo]
    public ImmutableArray<IntrospectionAspectRelationship> Predecessors
        => this._aspectInstance.Predecessors
            .Select( x => new IntrospectionAspectRelationship( x.Kind, this.Factory.GetIntrospectionAspectPredecessor( x.Instance ) ) )
            .ToImmutableArray();

    [Memo]
    ImmutableArray<IntrospectionAspectRelationship> IIntrospectionAspectPredecessor.Successors
        => this._successors
            .SelectAsImmutableArray(
                x => new IntrospectionAspectRelationship( x.Kind, this.Factory.GetIntrospectionAspectInstance( (IAspectInstance) x.Instance ) ) );

    public override string ToString() => $"'{this._aspectInstance.AspectClass.ShortName}' on '{this.TargetDeclaration}'";
}