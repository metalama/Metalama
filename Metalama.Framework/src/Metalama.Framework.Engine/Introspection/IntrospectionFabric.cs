// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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