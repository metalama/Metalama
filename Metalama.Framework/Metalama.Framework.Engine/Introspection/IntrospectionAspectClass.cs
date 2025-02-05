// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Introspection;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Introspection;

internal sealed class IntrospectionAspectClass : BaseIntrospectionAspectClass
{
    private readonly IntrospectionFactory _factory;
    private readonly ImmutableArray<AspectInstanceResult> _aspectInstanceResults;

    public IntrospectionAspectClass(
        IAspectClass aspectClass,
        ImmutableArray<AspectInstanceResult> aspectInstanceResults,
        IntrospectionFactory factory )
        : base( aspectClass )
    {
        this._aspectInstanceResults = aspectInstanceResults;
        this._factory = factory;
    }

    [Memo]
    public override ImmutableArray<IIntrospectionAspectInstance> Instances
        => this._aspectInstanceResults.Select( x => this._factory.GetIntrospectionAspectInstance( x.AspectInstance ) )
            .ToImmutableArray<IIntrospectionAspectInstance>();
}