// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Fabrics;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Introspection;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Introspection;

internal sealed class IntrospectionFabric : IIntrospectionFabric, IIntrospectionAspectPredecessorInternal
{
    private readonly FabricInstance _fabric;
    private readonly ICompilation _compilation;
    private readonly IntrospectionFactory _factory;
    private readonly ConcurrentLinkedList<AspectPredecessor> _successors = new();

    public IntrospectionFabric( FabricInstance fabric, ICompilation compilation, IntrospectionFactory factory )
    {
        this._fabric = fabric;
        this._compilation = compilation;
        this._factory = factory;
    }

    public int PredecessorDegree => 0;

    public IDeclaration TargetDeclaration => this._fabric.TargetDeclaration.GetTarget( this._compilation );

    public string FullName => this._fabric.Fabric.GetType().FullName!;

    public ImmutableArray<IntrospectionAspectRelationship> Predecessors => ImmutableArray<IntrospectionAspectRelationship>.Empty;

    [Memo]
    public ImmutableArray<IntrospectionAspectRelationship> Successors
        => this._successors.SelectAsImmutableArray(
            x => new IntrospectionAspectRelationship( x.Kind, this._factory.GetIntrospectionAspectInstance( (IAspectInstance) x.Instance ) ) );

    public void AddSuccessor( AspectPredecessor aspectInstance ) => this._successors.Add( aspectInstance );
}