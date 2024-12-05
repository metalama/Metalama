// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Aspects;

internal sealed class AggregateAspectInstance : IAspectInstanceInternal
{
    private readonly AspectInstance _primaryInstance;
    private readonly IReadOnlyList<AspectInstance> _otherInstances;

    private AggregateAspectInstance( AspectInstance firstInstance, List<AspectInstance> otherInstances )
    {
        this._primaryInstance = firstInstance;
        this._otherInstances = otherInstances;
    }

    public static IAspectInstanceInternal GetInstance( IEnumerable<AspectInstance> aspectInstances )
    {
        var instancesList = aspectInstances.ToMutableList();

        switch ( instancesList.Count )
        {
            case 0:
                throw new AssertionFailedException( "The collection is empty." );

            case 1:
                return instancesList[0];

            default:
                {
                    instancesList.Sort();

                    var firstInstance = instancesList[0];
                    instancesList.RemoveAt( 0 );

                    return new AggregateAspectInstance( firstInstance, instancesList );
                }
        }
    }

    public IAspect Aspect => this._primaryInstance.Aspect;

    IRef<IDeclaration> IAspectPredecessor.TargetDeclaration => this._primaryInstance.TargetDeclaration;

    public IAspectClassImpl AspectClass => this._primaryInstance.AspectClass;

    IAspectClass IAspectInstance.AspectClass => this.AspectClass;

    public bool IsSkipped => this._primaryInstance.IsSkipped;

    public bool IsInheritable => this._primaryInstance.IsInheritable;

    [Memo]
    public ImmutableArray<IAspectInstance> SecondaryInstances => this._otherInstances.Cast<IAspectInstance>().ToImmutableArray();

    public ImmutableArray<AspectPredecessor> Predecessors => this._primaryInstance.Predecessors;

    public IAspectState? AspectState => this._primaryInstance.AspectState;

    public void SetState( IAspectState? value ) => this._primaryInstance.AspectState = value;

    public void Skip() => this._primaryInstance.Skip();

    public ImmutableDictionary<TemplateClass, TemplateClassInstance> TemplateInstances => this._primaryInstance.TemplateInstances;

    public FormattableString FormatPredecessor( ICompilation compilation ) => this._primaryInstance.FormatPredecessor( compilation );

    public Location? GetDiagnosticLocation( Compilation compilation ) => this._primaryInstance.GetDiagnosticLocation( compilation );

    public int TargetDeclarationDepth => this._primaryInstance.TargetDeclarationDepth;

    public int PredecessorDegree => this._primaryInstance.PredecessorDegree;

    public string DiagnosticSourceDescription => this._primaryInstance.DiagnosticSourceDescription;

    ImmutableArray<SyntaxTree> IAspectPredecessorImpl.PredecessorTreeClosure => this._primaryInstance.PredecessorTreeClosure;

    DeclarationOriginKind IDeclarationOrigin.Kind => DeclarationOriginKind.Aspect;

    bool IDeclarationOrigin.IsCompilerGenerated => true;

    IAspectInstance IAspectDeclarationOrigin.AspectInstance => this;
}