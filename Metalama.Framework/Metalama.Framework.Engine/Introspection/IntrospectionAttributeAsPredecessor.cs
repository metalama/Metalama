// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Introspection;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Introspection;

internal sealed class IntrospectionAttributeAsPredecessor : IIntrospectionAspectPredecessorInternal
{
    private readonly IntrospectionFactory _factory;
    private readonly IAttribute _attribute;
    private readonly ConcurrentLinkedList<AspectPredecessor> _successors = new();

    public IntrospectionAttributeAsPredecessor( IAttribute attribute, IntrospectionFactory factory )
    {
        this._factory = factory;
        this._attribute = attribute;
    }

    public int PredecessorDegree => 0;

    public IDeclaration TargetDeclaration => this._attribute.ContainingDeclaration;

    public ImmutableArray<IntrospectionAspectRelationship> Predecessors => ImmutableArray<IntrospectionAspectRelationship>.Empty;

    [Memo]
    public ImmutableArray<IntrospectionAspectRelationship> Successors
        => this._successors.SelectAsImmutableArray(
            x => new IntrospectionAspectRelationship(
                AspectPredecessorKind.Attribute,
                this._factory.GetIntrospectionAspectInstance( (IAspectInstance) x.Instance ) ) );

    public void AddSuccessor( AspectPredecessor aspectInstance ) => this._successors.Add( aspectInstance );

    public override string ToString() => this._attribute.ToString()!;
}