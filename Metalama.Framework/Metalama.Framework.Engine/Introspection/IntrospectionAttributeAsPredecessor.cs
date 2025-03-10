// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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